using BookingSystem.Application.Commands.BookingCommand.CreateBooking;
using BookingSystem.Application.Commands.BookingCommand.UpdateBooking;
using BookingSystem.Application.DTO;
using BookingSystem.Application.Queries.QueriesBooking.GetAllBookings;
using BookingSystem.Domain.Entities;
using BookingSystem.Domain.Enums;
using BookingSystem.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace BookingSystem.Test;

[TestFixture]
public class BookingReferralDataTests
{
    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    [Test]
    public async Task CreateAndGetBookings_PreservesReferralData()
    {
        using var context = CreateContext();
        var handler = new CreateBookingCommandHandler(context);

        var result = await handler.Handle(
            new CreateBookingCommand(CreateDto()),
            CancellationToken.None);

        var queryResult = await new GetAllBookingsQueryHandler(context)
            .Handle(new GetAllBookingsQuery(), CancellationToken.None);
        var booking = queryResult.Data!.Single(b => b.BookingId == result.Data!.BookingId);

        Assert.Multiple(() =>
        {
            Assert.That(booking.ReferralDepartment, Is.EqualTo("Avdelning 361"));
            Assert.That(booking.ReferralContactName, Is.EqualTo("Anna Sjöberg"));
            Assert.That(booking.ReferralContactPhone, Is.EqualTo("031-342 10 61"));
        });
    }

    [Test]
    public async Task UpdateBooking_PersistsChangedReferralData()
    {
        using var context = CreateContext();
        var booking = CreateEntity();
        context.Bookings.Add(booking);
        await context.SaveChangesAsync();

        var dto = CreateDto();
        dto.ReferralDepartment = "Avdelning 362";
        dto.ReferralContactName = "Erik Lund";
        dto.ReferralContactPhone = "031-342 10 62";

        var result = await new UpdateBookingCommandHandler(context)
            .Handle(new UpdateBookingCommand(booking.BookingId, dto), CancellationToken.None);
        var persisted = await context.Bookings.SingleAsync();

        Assert.That(result.Success, Is.True);
        Assert.Multiple(() =>
        {
            Assert.That(persisted.ReferralDepartment, Is.EqualTo("Avdelning 362"));
            Assert.That(persisted.ReferralContactName, Is.EqualTo("Erik Lund"));
            Assert.That(persisted.ReferralContactPhone, Is.EqualTo("031-342 10 62"));
        });
    }

    private static CreateBookingDto CreateDto()
    {
        return new CreateBookingDto
        {
            Date = new DateTime(2026, 6, 20, 9, 0, 0),
            PatientId = 1,
            TreatmentType = TreatmentType.EctPlanerad,
            CreatedById = 1,
            CreatedAt = DateTime.UtcNow,
            Priority = BookingPriority.Green,
            Status = BookingStatus.Pending,
            ReferralDepartment = "Avdelning 361",
            ReferralContactName = "Anna Sjöberg",
            ReferralContactPhone = "031-342 10 61"
        };
    }

    private static Booking CreateEntity()
    {
        return new Booking
        {
            Date = new DateTime(2026, 6, 20, 9, 15, 0),
            PatientId = 1,
            TreatmentType = TreatmentType.EctAkut,
            CreatedByUserId = 1,
            CreatedAt = DateTime.UtcNow,
            Priority = BookingPriority.Red,
            Status = BookingStatus.Pending
        };
    }
}
