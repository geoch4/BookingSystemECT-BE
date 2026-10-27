using System.Security.Claims;
using System.Text.Json;
using API;
using API.Controllers;
using BookingSystem.Application.Commands.BookingCommand.CreateBooking;
using BookingSystem.Application.Common;
using BookingSystem.Application.DTO;
using BookingSystem.Application.Validators;
using BookingSystem.Domain;
using BookingSystem.Domain.Entities;
using BookingSystem.Domain.Enums;
using BookingSystem.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BookingSystem.Test;

[TestFixture]
public class ReferralClinicalAndPersonnummerTests
{
    private static readonly DateTime Today = new(2026, 10, 27);
    private const string Department = "Psykiatri affektiva";
    private const string TreatmentUnit = "ECT SU";

    // ---- Personnummer ----

    [TestCase("19811218-9876", "19811218-9876")]
    [TestCase("198112189876", "19811218-9876")]
    [TestCase("811218-9876", "19811218-9876")]
    [TestCase("8112189876", "19811218-9876")]
    [TestCase("121212-1212", "20121212-1212")]
    [TestCase("121212+1212", "19121212-1212")]
    [TestCase("701063-2391", "19701063-2391")]
    public void Personnummer_AcceptsValidFormsAndNormalises(string input, string expected)
    {
        Assert.That(Personnummer.TryNormalize(input, Today, out var normalized), Is.True);
        Assert.That(normalized, Is.EqualTo(expected));
    }

    [TestCase("")]
    [TestCase("811218-9875")]     // wrong check digit
    [TestCase("811332-9876")]     // no 13th month
    [TestCase("19811218+9876")]   // '+' only makes sense with a two-digit year
    [TestCase("811218-98765")]
    [TestCase("abc")]
    [TestCase("00000000-0000")]
    public void Personnummer_RejectsInvalidInput(string input)
    {
        Assert.That(Personnummer.TryNormalize(input, Today, out _), Is.False);
    }

    [Test]
    public void PatientValidator_AcceptsTwelveDigitPersonnummer()
    {
        var result = new CreatePatientDtoValidator().Validate(new CreatePatientDto
        {
            FullName = "Test Testsson",
            SocialSecurityNumber = "19811218-9876"
        });

        Assert.That(result.IsValid, Is.True);
    }

    [Test]
    public async Task PatientsController_StoresNormalisedPersonnummer()
    {
        var mediator = new Moq.Mock<MediatR.IMediator>();
        CreatePatientDto? sent = null;
        mediator.Setup(m => m.Send(Moq.It.IsAny<BookingSystem.Application.Commands.PatientCommand.CreatePatient.CreatePatientCommand>(), Moq.It.IsAny<CancellationToken>()))
            .Callback<MediatR.IRequest<PatientDto>, CancellationToken>((cmd, _) =>
                sent = ((BookingSystem.Application.Commands.PatientCommand.CreatePatient.CreatePatientCommand)cmd).Dto)
            .ReturnsAsync(new PatientDto { PatientId = 7 });
        var controller = new PatientsController(mediator.Object, null!, null!) { ControllerContext = Context("Admin", null) };

        var result = await controller.Create(new CreatePatientDto { FullName = " Test Testsson ", SocialSecurityNumber = "8112189876" });

        Assert.That(result, Is.TypeOf<CreatedAtActionResult>());
        Assert.That(sent!.SocialSecurityNumber, Is.EqualTo("19811218-9876"));
        Assert.That(sent.FullName, Is.EqualTo("Test Testsson"));
    }

    [Test]
    public async Task PatientsController_RejectsInvalidPersonnummer()
    {
        var mediator = new Moq.Mock<MediatR.IMediator>(Moq.MockBehavior.Strict);
        var controller = new PatientsController(mediator.Object, null!, null!) { ControllerContext = Context("Admin", null) };

        var result = await controller.Create(new CreatePatientDto { FullName = "Test", SocialSecurityNumber = "811218-9875" });

        Assert.That(result, Is.TypeOf<BadRequestObjectResult>());
        mediator.VerifyNoOtherCalls();
    }

    // ---- Clinical details ----

    [Test]
    public async Task CreateBooking_PersistsRtmsClinicalDetails()
    {
        using var context = CreateContext();
        var dto = ReferralDto();
        dto.ClinicalDetails = RtmsDetails();

        var result = await new CreateBookingCommandHandler(context).Handle(new CreateBookingCommand(dto), CancellationToken.None);

        var stored = context.ReferralClinicalDetails.Single();
        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(stored.BookingId, Is.EqualTo(result.Data!.BookingId));
            Assert.That(stored.Indication, Is.EqualTo("Terapiresistent depression"));
            Assert.That(stored.PreviousTreatments, Is.EqualTo("Sertralin, venlafaxin"));
            Assert.That(stored.RtmsSessionPlan, Is.EqualTo("double"));
            Assert.That(stored.MetalImplantsOrDevices, Is.False);
            Assert.That(stored.SeizureHistory, Is.False);
            Assert.That(stored.ContraindicationChecklistComplete, Is.True);
            Assert.That(stored.CompletedDocuments, Is.EqualTo("contraindicationChecklist,implantCheck"));
        });
    }

    [Test]
    public async Task AuditLog_DoesNotCopyClinicalFreeText()
    {
        using var context = CreateContext();
        var dto = ReferralDto();
        dto.ClinicalDetails = RtmsDetails();

        await new CreateBookingCommandHandler(context).Handle(new CreateBookingCommand(dto), CancellationToken.None);

        var entry = context.AuditLogs.Single(a => a.EntityName == nameof(ReferralClinicalDetails));
        Assert.That(entry.ChangedFields, Does.Not.Contain("Terapiresistent"));
        Assert.That(entry.ChangedFields, Does.Not.Contain("Sertralin"));
        Assert.That(entry.ChangedFields, Does.Contain("[redacted]"));
    }

    [Test]
    public async Task GetClinicalDetails_ReturnsDetailsToTheTreatmentUnitAndLogsTheRead()
    {
        using var context = CreateContext();
        var bookingId = await CreateReferralWithDetails(context);
        var controller = Referrals(context, "Behandlingsenhet", TreatmentUnit);

        var result = await controller.GetClinicalDetails(bookingId.ToString(), CancellationToken.None);

        var ok = result.Result as OkObjectResult;
        Assert.That(ok, Is.Not.Null);
        var details = (ReferralClinicalDetailsDto)ok!.Value!;
        Assert.That(details.Indication, Is.EqualTo("Terapiresistent depression"));
        Assert.That(details.CompletedDocuments, Is.EquivalentTo(new[] { "contraindicationChecklist", "implantCheck" }));
        Assert.That(context.AuditLogs.Any(a => a.EntityName == nameof(ReferralClinicalDetails) && a.Action == "Viewed"), Is.True);
    }

    [Test]
    public async Task GetClinicalDetails_IsForbiddenForAnotherUnit()
    {
        using var context = CreateContext();
        var bookingId = await CreateReferralWithDetails(context);
        var controller = Referrals(context, "Behandlingsenhet", "ECT NU");

        var result = await controller.GetClinicalDetails(bookingId.ToString(), CancellationToken.None);

        Assert.That(result.Result, Is.TypeOf<ForbidResult>());
    }

    [Test]
    public void ClinicalValidator_RejectsUnknownSessionPlan()
    {
        var details = RtmsDetails();
        details.RtmsSessionPlan = "triple";

        Assert.That(new ReferralClinicalDetailsDtoValidator().Validate(details).IsValid, Is.False);
    }

    // ---- helpers ----

    private static async Task<int> CreateReferralWithDetails(AppDbContext context)
    {
        context.Patients.Add(new Patient { PatientId = 1, FullName = "Test", SocialSecurityNumber = "19811218-9876" });
        await context.SaveChangesAsync();
        var dto = ReferralDto();
        dto.ClinicalDetails = RtmsDetails();
        var result = await new CreateBookingCommandHandler(context).Handle(new CreateBookingCommand(dto), CancellationToken.None);
        return result.Data!.BookingId;
    }

    private static CreateBookingDto ReferralDto() => new()
    {
        Date = new DateTime(2026, 11, 2, 13, 0, 0),
        PatientId = 1,
        TreatmentType = TreatmentType.RtmsDepression,
        CreatedById = 1,
        CreatedAt = DateTime.UtcNow,
        Priority = BookingPriority.Green,
        Status = BookingStatus.Pending,
        ReferralDepartment = Department,
        DepartmentId = Department,
        TreatmentUnitId = TreatmentUnit
    };

    private static ReferralClinicalDetailsDto RtmsDetails() => new()
    {
        Indication = " Terapiresistent depression ",
        PreviousTreatments = "Sertralin, venlafaxin",
        TreatmentGoal = "Minskad depressionsgrad",
        RtmsSessionPlan = "double",
        ContraindicationChecklistComplete = true,
        MetalImplantsOrDevices = false,
        SeizureHistory = false,
        CompletedDocuments = ["contraindicationChecklist", "implantCheck"]
    };

    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options, new Actor());
    }

    private static ReferralsController Referrals(AppDbContext context, string role, string unit)
    {
        return new ReferralsController(context, new AuditLogWriter(context, new Actor()))
        {
            ControllerContext = Context(role, unit)
        };
    }

    private static ControllerContext Context(string role, string? unit)
    {
        var claims = new List<Claim> { new(ClaimTypes.Role, role), new(ClaimTypes.NameIdentifier, "1") };
        if (unit is not null) claims.Add(new Claim("care_unit", unit));
        return new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Test")) }
        };
    }

    private sealed class Actor : IAuditContext
    {
        public int? UserId => 1;
        public string? UserName => "Test";
        public string? Role => "Avdelning";
        public string? CareUnitId => Department;
        public string? IpAddress => null;
        public string? RequestMethod => "TEST";
        public string? RequestPath => "/test";
        public string? Reason => "Test";
    }
}
