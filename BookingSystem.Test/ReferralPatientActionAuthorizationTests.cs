using System.Security.Claims;
using API;
using API.Controllers;
using BookingSystem.Application.DTO;
using BookingSystem.Domain;
using BookingSystem.Domain.Entities;
using BookingSystem.Domain.Enums;
using BookingSystem.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BookingSystem.Test;

public class ReferralPatientActionAuthorizationTests
{
    private const string OwnCareUnit = "Avdelning 361";
    private const string OtherCareUnit = "Avdelning 362";

    [Test]
    public async Task Department_User_Can_Record_Patient_Action_For_Own_Care_Unit()
    {
        using var context = CreateContext();
        var booking = AddBooking(context, OwnCareUnit);
        await context.SaveChangesAsync();
        var controller = CreateController(context, "Avdelning", OwnCareUnit);

        var result = await controller.RecordPatientAction(
            booking.BookingId.ToString(),
            CreatePatientActionDto(),
            CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.Result, Is.TypeOf<OkObjectResult>());
            Assert.That(context.ReferralActions.Count(), Is.EqualTo(1));
        });
    }

    [Test]
    public async Task Department_User_Cannot_Record_Patient_Action_For_Another_Care_Unit()
    {
        using var context = CreateContext();
        var booking = AddBooking(context, OtherCareUnit);
        await context.SaveChangesAsync();
        var controller = CreateController(context, "Avdelning", OwnCareUnit);

        var result = await controller.RecordPatientAction(
            booking.BookingId.ToString(),
            CreatePatientActionDto(),
            CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.Result, Is.TypeOf<ForbidResult>());
            Assert.That(context.ReferralActions.Count(), Is.EqualTo(0));
        });
    }

    [Test]
    public async Task Medical_Responsible_Cannot_Record_Department_Patient_Action()
    {
        using var context = CreateContext();
        var booking = AddBooking(context, OwnCareUnit);
        await context.SaveChangesAsync();
        var controller = CreateController(context, "Överläkare", null);

        var result = await controller.RecordPatientAction(
            booking.BookingId.ToString(),
            CreatePatientActionDto(),
            CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.Result, Is.TypeOf<ForbidResult>());
            Assert.That(context.ReferralActions.Count(), Is.EqualTo(0));
        });
    }

    [TestCase("Unknown", OwnCareUnit)]
    [TestCase("Avdelning", null)]
    [TestCase("Avdelning", OtherCareUnit)]
    public async Task Reads_Deny_Unknown_Missing_Or_Other_Scope(string role, string? unit)
    {
        using var context = CreateContext();
        var booking = AddBooking(context, OwnCareUnit);
        await context.SaveChangesAsync();
        var controller = CreateController(context, role, unit);
        Assert.That((await controller.GetActions(booking.BookingId.ToString(), CancellationToken.None)).Result, Is.TypeOf<ForbidResult>());
        Assert.That((await controller.GetTimeline(booking.BookingId.ToString(), CancellationToken.None)).Result, Is.TypeOf<ForbidResult>());
    }

    [TestCase("Behandlingsenhet")]
    [TestCase("Medicinskt ansvarig")]
    [TestCase("Admin")]
    public async Task Authorized_Staff_Can_Read_Actions_And_Timeline(string role)
    {
        using var context = CreateContext();
        var booking = AddBooking(context, OwnCareUnit);
        await context.SaveChangesAsync();
        var controller = CreateController(context, role, OwnCareUnit);
        Assert.That((await controller.GetActions(booking.BookingId.ToString(), CancellationToken.None)).Result, Is.TypeOf<OkObjectResult>());
        Assert.That((await controller.GetTimeline(booking.BookingId.ToString(), CancellationToken.None)).Result, Is.TypeOf<OkObjectResult>());
    }

    [TestCase("Behandlingsenhet", OwnCareUnit)]
    [TestCase("Medicinskt ansvarig", OtherCareUnit)]
    [TestCase("Medicinskt ansvarig", null)]
    public async Task Prioritization_Denies_Wrong_Role_Or_Scope(string role, string? unit)
    {
        using var context = CreateContext();
        var booking = AddBooking(context, OwnCareUnit);
        await context.SaveChangesAsync();
        var controller = CreateController(context, role, unit);
        var result = await controller.Prioritize(booking.BookingId.ToString(), new CreateReferralActionDto { Priority = BookingPriority.Red, Comment = "Test" }, CancellationToken.None);
        Assert.That(result.Result, Is.TypeOf<ForbidResult>());
        Assert.That(booking.Priority, Is.EqualTo(BookingPriority.Green));
        Assert.That(context.ReferralActions.Count(), Is.Zero);
    }

    [Test]
    public async Task Queue_Returns_Only_Assigned_Unit()
    {
        using var context = CreateContext();
        var own = AddBooking(context, OwnCareUnit);
        AddBooking(context, OtherCareUnit);
        await context.SaveChangesAsync();
        var result = await CreateController(context, "Behandlingsenhet", OwnCareUnit).GetQueue(CancellationToken.None);
        var rows = ((OkObjectResult)result.Result!).Value as IEnumerable<ReferralDto>;
        Assert.That(rows!.Select(r => r.BookingId), Is.EqualTo(new[] { own.BookingId }));
    }

    [TestCase("Behandlingsenhet")]
    [TestCase("Medicinskt ansvarig")]
    [TestCase("Avdelning")]
    [TestCase("Unknown")]
    public async Task Generic_Booking_Update_And_Delete_Cannot_Bypass_Action_Permissions(string role)
    {
        var mediator = new Moq.Mock<MediatR.IMediator>(Moq.MockBehavior.Strict);
        var controller = new BookingController(mediator.Object, null!, null!);
        controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext {
            User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.Role, role), new Claim("care_unit", OwnCareUnit) }, "Test"))
        }};
        Assert.That(await controller.UpdateBooking(1, new CreateBookingDto()), Is.TypeOf<ForbidResult>());
        Assert.That(await controller.DeleteBooking(1), Is.TypeOf<ForbidResult>());
        mediator.VerifyNoOtherCalls();
    }

    [Test]
    public async Task Confirmation_Cannot_Change_Clinical_Priority_As_Treatment_Staff()
    {
        using var context = CreateContext();
        var booking = AddBooking(context, OwnCareUnit);
        await context.SaveChangesAsync();
        var oldDate = booking.Date;
        var result = await CreateController(context, "Behandlingsenhet", OwnCareUnit).ConfirmTime(booking.BookingId.ToString(), new CreateReferralActionDto {
            Priority = BookingPriority.Red, ConfirmedDateTime = oldDate.AddHours(1), Comment = "Test"
        }, CancellationToken.None);
        Assert.That(result.Result, Is.TypeOf<ForbidResult>());
        Assert.That(booking.Priority, Is.EqualTo(BookingPriority.Green));
        Assert.That(booking.Date, Is.EqualTo(oldDate));
        Assert.That(context.ReferralActions.Count(), Is.Zero);
    }

    [Test]
    public async Task Creating_Booking_Cannot_Grant_Access_To_Another_Units_Patient()
    {
        using var context = CreateContext();
        context.Patients.Add(new Patient { PatientId = 1, FullName = "Other patient", SocialSecurityNumber = "Test" });
        AddBooking(context, OtherCareUnit);
        await context.SaveChangesAsync();
        // Remove fixture creation evidence from the actor's unit; this patient belongs to the other unit.
        context.AuditLogs.RemoveRange(context.AuditLogs);
        await context.SaveChangesAsync();
        var mediator = new Moq.Mock<MediatR.IMediator>(Moq.MockBehavior.Strict);
        var controller = new BookingController(mediator.Object, null!, context);
        controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext {
            User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.Role, "Avdelning"), new Claim("care_unit", OwnCareUnit) }, "Test"))
        }};
        Assert.That(await controller.CreateBooking(new CreateBookingDto { PatientId = 1 }), Is.TypeOf<ForbidResult>());
        mediator.VerifyNoOtherCalls();
    }

    [Test]
    public async Task Patient_With_Explicit_Membership_Has_Unit_Access()
    {
        using var context = CreateContext();
        var patient = new Patient { FullName = "New patient", SocialSecurityNumber = "Test" };
        context.Patients.Add(patient);
        context.PatientCareUnits.Add(new PatientCareUnit {Patient=patient,UnitId=OwnCareUnit,Kind="department"});
        await context.SaveChangesAsync();
        Assert.That(await CareUnitPatientAccess.Query(context, OwnCareUnit).AnyAsync(p => p.PatientId == patient.PatientId), Is.True);
        Assert.That(await CareUnitPatientAccess.Query(context, OtherCareUnit).AnyAsync(p => p.PatientId == patient.PatientId), Is.False);
    }

    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options, new TestAuditContext());
    }

    private static ReferralsController CreateController(AppDbContext context, string role, string? careUnitId)
    {
        var controller = new ReferralsController(context, new AuditLogWriter(context, new TestAuditContext()));
        var claims = new List<Claim>
        {
            new(ClaimTypes.Role, role),
            new(ClaimTypes.NameIdentifier, "1"),
            new(ClaimTypes.Email, "test@example.se")
        };
        if (!string.IsNullOrWhiteSpace(careUnitId))
            claims.Add(new Claim("care_unit", careUnitId));

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"))
            }
        };

        return controller;
    }

    private static Booking AddBooking(AppDbContext context, string careUnitId)
    {
        var booking = new Booking
        {
            Date = new DateTime(2026, 10, 7, 9, 0, 0),
            PatientId = 1,
            TreatmentType = TreatmentType.EctPlanerad,
            CreatedByUserId = 1,
            CreatedAt = DateTime.UtcNow,
            Priority = BookingPriority.Green,
            Status = BookingStatus.Confirmed,
            ReferralDepartment = careUnitId,
            CareUnitId = careUnitId,
            DepartmentId = careUnitId,
            TreatmentUnitId = careUnitId
        };

        if (!context.Patients.Local.Any(p=>p.PatientId==1)) context.Patients.Add(new Patient {PatientId=1});
        if (!context.PatientCareUnits.Local.Any(m=>m.PatientId==1 && m.UnitId==careUnitId)) {
            context.PatientCareUnits.Add(new PatientCareUnit {PatientId=1,UnitId=careUnitId,Kind="department"});
            context.PatientCareUnits.Add(new PatientCareUnit {PatientId=1,UnitId=careUnitId,Kind="treatment"});
        }
        context.Bookings.Add(booking);
        return booking;
    }

    private static CreateReferralActionDto CreatePatientActionDto()
    {
        return new CreateReferralActionDto
        {
            ActionType = ReferralActionType.PatientPaused,
            Comment = "Testkommentar"
        };
    }

    private sealed class TestAuditContext : IAuditContext
    {
        public int? UserId => 1;
        public string? UserName => "test@example.se";
        public string? Role => "Test";
        public string? CareUnitId => OwnCareUnit;
        public string? IpAddress => "127.0.0.1";
        public string? RequestMethod => "TEST";
        public string? RequestPath => "/test";
        public string? Reason => "Test";
    }
}
