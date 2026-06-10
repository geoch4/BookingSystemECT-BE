using BookingSystem.Application.Commands.BookingCommand.CreateBooking;
using BookingSystem.Application.DTO;
using BookingSystem.Application.Queries.QueriesBooking.GetAllBookings;
using BookingSystem.Domain.Entities;
using BookingSystem.Domain.Enums;
using BookingSystem.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace BookingSystem.Test;

[TestFixture]
public class OperationalDataTests
{
    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    [Test]
    public void Model_HasUniqueStaffingDateAndActiveStandbyPatientIndexes()
    {
        using var context = CreateContext();
        var staffingIndex = context.Model.FindEntityType(typeof(StaffingStatus))!
            .GetIndexes()
            .Single();
        var standbyIndex = context.Model.FindEntityType(typeof(StandbyEntry))!
            .GetIndexes()
            .Single();

        Assert.Multiple(() =>
        {
            Assert.That(staffingIndex.IsUnique, Is.True);
            Assert.That(standbyIndex.IsUnique, Is.True);
            Assert.That(standbyIndex.GetFilter(), Is.EqualTo("[IsActive] = 1"));
        });
    }

    [Test]
    public async Task BookingApiData_PreservesClinicalAssessmentStatuses()
    {
        using var context = CreateContext();
        var dto = new CreateBookingDto
        {
            Date = new DateTime(2026, 6, 25, 9, 0, 0),
            PatientId = 1,
            TreatmentType = TreatmentType.EctPlanerad,
            CreatedById = 1,
            CreatedAt = DateTime.UtcNow,
            Priority = BookingPriority.Green,
            Status = BookingStatus.Pending,
            HealthDeclarationStatus = AssessmentStatus.Approved,
            AnesthesiaAssessmentStatus = AssessmentStatus.InProgress
        };

        await new CreateBookingCommandHandler(context)
            .Handle(new CreateBookingCommand(dto), CancellationToken.None);
        var result = await new GetAllBookingsQueryHandler(context)
            .Handle(new GetAllBookingsQuery(), CancellationToken.None);
        var booking = result.Data!.Single();

        Assert.Multiple(() =>
        {
            Assert.That(booking.HealthDeclarationStatus, Is.EqualTo(AssessmentStatus.Approved));
            Assert.That(booking.AnesthesiaAssessmentStatus, Is.EqualTo(AssessmentStatus.InProgress));
        });
    }
}
