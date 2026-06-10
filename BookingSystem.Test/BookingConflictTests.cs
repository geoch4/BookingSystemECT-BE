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
    public async Task CreateBooking_RejectsAnOccupiedActiveTime()
    {
        using var context = CreateContext();
        var date = new DateTime(2026, 6, 15, 9, 0, 0);
        context.Bookings.Add(CreateBookingEntity(date, BookingStatus.Confirmed));
        await context.SaveChangesAsync();

        var handler = new CreateBookingCommandHandler(context);
        var result = await handler.Handle(
            new CreateBookingCommand(CreateDto(date, BookingStatus.Pending)),
            CancellationToken.None);

        Assert.That(result.Success, Is.False);
        Assert.That(result.ErrorMessage, Is.EqualTo(BookingErrors.SlotOccupied));
        Assert.That(context.Bookings.Count(), Is.EqualTo(1));
    }

    [Test]
    public async Task CreateBooking_RejectsTheSameMinuteWithDifferentSeconds()
    {
        using var context = CreateContext();
        var occupiedDate = new DateTime(2026, 6, 15, 9, 0, 0);
        context.Bookings.Add(CreateBookingEntity(occupiedDate, BookingStatus.Confirmed));
        await context.SaveChangesAsync();

        var handler = new CreateBookingCommandHandler(context);
        var result = await handler.Handle(
            new CreateBookingCommand(CreateDto(occupiedDate.AddSeconds(45), BookingStatus.Pending)),
            CancellationToken.None);

        Assert.That(result.Success, Is.False);
        Assert.That(result.ErrorMessage, Is.EqualTo(BookingErrors.SlotOccupied));
    }

    [Test]
    public async Task CreateBooking_AllowsAPreviouslyCancelledTime()
    {
        using var context = CreateContext();
        var date = new DateTime(2026, 6, 15, 9, 15, 0);
        context.Bookings.Add(CreateBookingEntity(date, BookingStatus.Cancelled));
        await context.SaveChangesAsync();

        var handler = new CreateBookingCommandHandler(context);
        var result = await handler.Handle(
            new CreateBookingCommand(CreateDto(date, BookingStatus.Confirmed)),
            CancellationToken.None);

        Assert.That(result.Success, Is.True);
        Assert.That(context.Bookings.Count(), Is.EqualTo(2));
    }

    [Test]
    public async Task UpdateBooking_RejectsMovingToAnOccupiedActiveTime()
    {
        using var context = CreateContext();
        var occupiedDate = new DateTime(2026, 6, 15, 9, 30, 0);
        var bookingToMove = CreateBookingEntity(new DateTime(2026, 6, 15, 9, 45, 0), BookingStatus.Pending);
        context.Bookings.AddRange(
            CreateBookingEntity(occupiedDate, BookingStatus.Confirmed),
            bookingToMove);
        await context.SaveChangesAsync();

        var handler = new UpdateBookingCommandHandler(context);
        var result = await handler.Handle(
            new UpdateBookingCommand(
                bookingToMove.BookingId,
                CreateDto(occupiedDate, BookingStatus.Confirmed)),
            CancellationToken.None);

        Assert.That(result.Success, Is.False);
        Assert.That(result.ErrorMessage, Is.EqualTo(BookingErrors.SlotOccupied));
    }

    [Test]
    public void BookingDateIndex_IsUniqueForNonCancelledBookings()
    {
        using var context = CreateContext();
        var index = context.Model.FindEntityType(typeof(Booking))!
            .GetIndexes()
            .Single(i => i.Properties.Select(p => p.Name).SequenceEqual([nameof(Booking.Date)]));

        Assert.That(index.IsUnique, Is.True);
        Assert.That(index.GetFilter(), Is.EqualTo("[Status] <> 2"));
    }

    private static Booking CreateBookingEntity(DateTime date, BookingStatus status)
    {
        return new Booking
        {
            Date = date,
            PatientId = 1,
            TreatmentType = TreatmentType.EctAkut,
            CreatedByUserId = 1,
            CreatedAt = DateTime.UtcNow,
            Priority = BookingPriority.Green,
            Status = status
        };
    }

    private static CreateBookingDto CreateDto(DateTime date, BookingStatus status)
    {
        return new CreateBookingDto
        {
            Date = date,
            PatientId = 2,
            TreatmentType = TreatmentType.EctPlanerad,
            CreatedById = 1,
            CreatedAt = DateTime.UtcNow,
            Priority = BookingPriority.Green,
            Status = status
        };
    }
}
