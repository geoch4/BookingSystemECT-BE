using BookingSystem.Application.Commands.BookingCommand.CreateBooking;
using BookingSystem.Application.Commands.BookingCommand.UpdateBooking;
using BookingSystem.Application.Common;
using BookingSystem.Application.DTO;
using BookingSystem.Domain.Entities;
using BookingSystem.Domain.Enums;
using BookingSystem.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace BookingSystem.Test;

[TestFixture]
public class BookingConflictTests
{
    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    [Test]
    public async Task CreateBooking_AllowsTwoPendingReferralsOnTheSameRequestedDay()
    {
        // Regression: departments asking for the same start day used to get 409.
        using var context = CreateContext();
        var handler = new CreateBookingCommandHandler(context);
        var requested = new DateTime(2026, 11, 2, 9, 0, 0);

        var first = await handler.Handle(new CreateBookingCommand(CreateDto(requested, BookingStatus.Pending, null)), CancellationToken.None);
        var second = await handler.Handle(new CreateBookingCommand(CreateDto(requested, BookingStatus.Pending, null)), CancellationToken.None);

        Assert.That(first.Success, Is.True);
        Assert.That(second.Success, Is.True);
        Assert.That(context.Bookings.Count(), Is.EqualTo(2));
    }

    [Test]
    public async Task CreateBooking_PendingReferralIsNotBlockedByAConfirmedBooking()
    {
        using var context = CreateContext();
        var date = new DateTime(2026, 6, 15, 9, 0, 0);
        context.Bookings.Add(CreateBookingEntity(date, BookingStatus.Confirmed, "ECT SU"));
        await context.SaveChangesAsync();

        var result = await new CreateBookingCommandHandler(context).Handle(
            new CreateBookingCommand(CreateDto(date, BookingStatus.Pending, "ECT SU")),
            CancellationToken.None);

        Assert.That(result.Success, Is.True);
    }

    [Test]
    public async Task CreateBooking_StoresTheRequestedStartDay()
    {
        using var context = CreateContext();
        var dto = CreateDto(new DateTime(2026, 11, 2, 9, 0, 0), BookingStatus.Pending, null);
        dto.RequestedStartDate = new DateTime(2026, 11, 4, 15, 30, 0);

        var result = await new CreateBookingCommandHandler(context).Handle(new CreateBookingCommand(dto), CancellationToken.None);

        Assert.That(result.Success, Is.True);
        Assert.That(context.Bookings.Single().RequestedStartDate, Is.EqualTo(new DateTime(2026, 11, 4)));
        Assert.That(result.Data!.RequestedStartDate, Is.EqualTo(new DateTime(2026, 11, 4)));
    }

    [Test]
    public async Task CreateBooking_RejectsAConfirmedBookingInAnOccupiedSlotAtTheSameUnit()
    {
        using var context = CreateContext();
        var date = new DateTime(2026, 6, 15, 9, 0, 0);
        context.Bookings.Add(CreateBookingEntity(date, BookingStatus.Confirmed, "ECT SU"));
        await context.SaveChangesAsync();

        var result = await new CreateBookingCommandHandler(context).Handle(
            new CreateBookingCommand(CreateDto(date.AddSeconds(45), BookingStatus.Confirmed, "ECT SU")),
            CancellationToken.None);

        Assert.That(result.Success, Is.False);
        Assert.That(result.ErrorMessage, Is.EqualTo(BookingErrors.SlotOccupied));
        Assert.That(context.Bookings.Count(), Is.EqualTo(1));
    }

    [Test]
    public async Task CreateBooking_AllowsTheSameMinuteAtAnotherUnit()
    {
        using var context = CreateContext();
        var date = new DateTime(2026, 6, 15, 9, 0, 0);
        context.Bookings.Add(CreateBookingEntity(date, BookingStatus.Confirmed, "ECT SU"));
        await context.SaveChangesAsync();

        var result = await new CreateBookingCommandHandler(context).Handle(
            new CreateBookingCommand(CreateDto(date, BookingStatus.Confirmed, "ECT NU")),
            CancellationToken.None);

        Assert.That(result.Success, Is.True);
    }

    [Test]
    public async Task CreateBooking_AllowsAPreviouslyCancelledTime()
    {
        using var context = CreateContext();
        var date = new DateTime(2026, 6, 15, 9, 15, 0);
        context.Bookings.Add(CreateBookingEntity(date, BookingStatus.Cancelled, "ECT SU"));
        await context.SaveChangesAsync();

        var result = await new CreateBookingCommandHandler(context).Handle(
            new CreateBookingCommand(CreateDto(date, BookingStatus.Confirmed, "ECT SU")),
            CancellationToken.None);

        Assert.That(result.Success, Is.True);
        Assert.That(context.Bookings.Count(), Is.EqualTo(2));
    }

    [Test]
    public async Task UpdateBooking_RejectsConfirmingIntoAnOccupiedSlotAtTheSameUnit()
    {
        using var context = CreateContext();
        var occupiedDate = new DateTime(2026, 6, 15, 9, 30, 0);
        var bookingToMove = CreateBookingEntity(new DateTime(2026, 6, 15, 9, 45, 0), BookingStatus.Pending, "ECT SU");
        context.Bookings.AddRange(
            CreateBookingEntity(occupiedDate, BookingStatus.Confirmed, "ECT SU"),
            bookingToMove);
        await context.SaveChangesAsync();

        var result = await new UpdateBookingCommandHandler(context).Handle(
            new UpdateBookingCommand(bookingToMove.BookingId, CreateDto(occupiedDate, BookingStatus.Confirmed, "ECT SU")),
            CancellationToken.None);

        Assert.That(result.Success, Is.False);
        Assert.That(result.ErrorMessage, Is.EqualTo(BookingErrors.SlotOccupied));
    }

    [Test]
    public async Task UpdateBooking_AllowsResavingAConfirmedBookingInItsOwnSlot()
    {
        using var context = CreateContext();
        var date = new DateTime(2026, 6, 15, 10, 0, 0);
        var booking = CreateBookingEntity(date, BookingStatus.Confirmed, "ECT SU");
        context.Bookings.Add(booking);
        await context.SaveChangesAsync();

        var result = await new UpdateBookingCommandHandler(context).Handle(
            new UpdateBookingCommand(booking.BookingId, CreateDto(date, BookingStatus.Confirmed, "ECT SU")),
            CancellationToken.None);

        Assert.That(result.Success, Is.True);
    }

    [Test]
    public void SlotIndex_IsUniquePerTreatmentUnitForConfirmedBookingsOnly()
    {
        using var context = CreateContext();
        var index = context.Model.FindEntityType(typeof(Booking))!
            .GetIndexes()
            .Single(i => i.Properties.Select(p => p.Name)
                .SequenceEqual([nameof(Booking.TreatmentUnitId), nameof(Booking.Date)]));

        Assert.That(index.IsUnique, Is.True);
        Assert.That(index.GetFilter(), Is.EqualTo("[Status] = 1"));
        Assert.That(context.Model.FindEntityType(typeof(Booking))!.GetIndexes()
            .Any(i => i.Properties.Select(p => p.Name).SequenceEqual([nameof(Booking.Date)])), Is.False);
    }

    private static Booking CreateBookingEntity(DateTime date, BookingStatus status, string? treatmentUnitId)
    {
        return new Booking
        {
            Date = date,
            PatientId = 1,
            TreatmentType = TreatmentType.EctAkut,
            CreatedByUserId = 1,
            CreatedAt = DateTime.UtcNow,
            Priority = BookingPriority.Green,
            Status = status,
            TreatmentUnitId = treatmentUnitId
        };
    }

    private static CreateBookingDto CreateDto(DateTime date, BookingStatus status, string? treatmentUnitId)
    {
        return new CreateBookingDto
        {
            Date = date,
            PatientId = 2,
            TreatmentType = TreatmentType.EctPlanerad,
            CreatedById = 1,
            CreatedAt = DateTime.UtcNow,
            Priority = BookingPriority.Green,
            Status = status,
            TreatmentUnitId = treatmentUnitId
        };
    }
}
