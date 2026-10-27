// Backend purpose: Exposes HTTP endpoints for the PatientController backend feature.
using BookingSystem.Application.Commands.PatientCommand.CreatePatient;
using BookingSystem.Application.Commands.PatientCommand.DeletePatient;
using BookingSystem.Application.Commands.PatientCommand.UpdatePatient;
using BookingSystem.Application.Authorization;
using BookingSystem.Application.Common;
using BookingSystem.Application.Validators;
using BookingSystem.Application.DTO;
using BookingSystem.Application.Mappings;
using BookingSystem.Application.Queries.QueriesPatient.GetAllPatient;
using BookingSystem.Application.Queries.QueriesPatient.GetPatientById;
using BookingSystem.Infrastructure;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace API.Controllers;

// TODO: Return OperationResult consistently from this controller flow.
// TODO: Align the corresponding MediatR handlers with OperationResult.

[ApiController]
[Authorize(Policy = AccessPolicies.CareStaff)]
[Route("api/[controller]")]
public class PatientsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly AuditLogWriter _auditLogWriter;
    private readonly AppDbContext _context;
    private static readonly CreatePatientDtoValidator Validator = new();

    public PatientsController(IMediator mediator, AuditLogWriter auditLogWriter, AppDbContext context)
    {
        _mediator = mediator;
        _auditLogWriter = auditLogWriter;
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var patients = CanManageAllPatients()
            ? await _mediator.Send(new GetAllPatientsQuery(), cancellationToken)
            : await GetPatientsForCurrentCareUnit(cancellationToken);

        await _auditLogWriter.RecordReadAsync("Patient", "list", cancellationToken);
        return Ok(patients);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
    {
        var patient = await _mediator.Send(new GetPatientByIdQuery(id));
        if (patient == null) return NotFound();
        if (!await CanAccessPatient(id, cancellationToken)) return Forbid();

        await _auditLogWriter.RecordReadAsync("Patient", id.ToString(), cancellationToken);
        return Ok(patient);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreatePatientDto dto)
    {
        var role = User.FindFirstValue(ClaimTypes.Role);
        if (!AccessRoles.IsAdmin(role) && !(AccessRoles.IsDepartmentUser(role) && !string.IsNullOrWhiteSpace(GetCareUnitId()))) return Forbid();
        var invalid = await ValidateAndNormalize(dto);
        if (invalid is not null) return invalid;
        var created = await _mediator.Send(new CreatePatientCommand(dto));
        return CreatedAtAction(nameof(GetById), new { id = created.PatientId }, created);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] CreatePatientDto dto)
    {
        if (!CanManageAllPatients()) return Forbid();
        var invalid = await ValidateAndNormalize(dto);
        if (invalid is not null) return invalid;

        var success = await _mediator.Send(new UpdatePatientCommand(id, dto));
        if (!success) return NotFound();
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        if (!CanManageAllPatients()) return Forbid();

        var success = await _mediator.Send(new DeletePatientCommand(id));
        if (!success) return NotFound();
        return Ok(new { message = "Patient successfully deleted" });
    }

    [HttpGet("{id}/care-units")]
    public async Task<IActionResult> GetCareUnits(int id)
    {
        if (!CanManageAllPatients()) return Forbid();
        return Ok(await _context.PatientCareUnits.Where(m=>m.PatientId==id).Select(m=>new {m.UnitId,m.Kind,m.IsActive,m.UpdatedAt,m.Reason}).ToListAsync());
    }
    public record PatientMembershipDto(string UnitId, string Kind, bool IsActive, string Reason);
    [HttpPut("{id}/care-units")]
    public async Task<IActionResult> UpdateCareUnit(int id, [FromBody] PatientMembershipDto dto)
    {
        if (!CanManageAllPatients()) return Forbid();
        if (string.IsNullOrWhiteSpace(dto.UnitId) || dto.UnitId.Length>120 || dto.Kind is not ("department" or "treatment") || string.IsNullOrWhiteSpace(dto.Reason) || dto.Reason.Length>500) return BadRequest();
        if (!await _context.Patients.AnyAsync(p=>p.PatientId==id)) return NotFound();
        if (dto.IsActive)
        {
            var users = await _context.Users.Include(u=>u.Role).ToListAsync();
            if (!users.Any(u=>u.CareUnitId==dto.UnitId && (dto.Kind=="department" ? AccessRoles.IsDepartmentUser(u.Role?.RoleName) : !AccessRoles.IsAdmin(u.Role?.RoleName) && AccessRoles.IsReferralDecisionUser(u.Role?.RoleName)))) return BadRequest(new {message="Okänd enhet för vald enhetstyp."});
            await PatientUnitMemberships.Grant(_context,id,dto.UnitId,dto.Kind,dto.Reason);
        }
        else
        {
            var membership = await _context.PatientCareUnits.FindAsync(id,dto.UnitId,dto.Kind);
            if (membership is null) return NotFound();
            membership.IsActive=false; membership.UpdatedAt=DateTime.UtcNow; membership.Reason=dto.Reason;
        }
        await _context.SaveChangesAsync();
        return NoContent();
    }

    public record TransferPatientDto(string DepartmentId, string Reason);
    [HttpPost("{id}/transfer")]
    public async Task<IActionResult> Transfer(int id, [FromBody] TransferPatientDto dto)
    {
        if (!CanManageAllPatients()) return Forbid();
        if (string.IsNullOrWhiteSpace(dto.DepartmentId) || dto.DepartmentId.Length>120 || string.IsNullOrWhiteSpace(dto.Reason) || dto.Reason.Length>500) return BadRequest();
        if (!await _context.Patients.AnyAsync(p=>p.PatientId==id)) return NotFound();
        var users = await _context.Users.Include(u=>u.Role).ToListAsync();
        if (!users.Any(u=>u.CareUnitId==dto.DepartmentId && AccessRoles.IsDepartmentUser(u.Role?.RoleName))) return BadRequest(new {message="Okänd avdelning."});
        var old = await _context.PatientCareUnits.Where(m=>m.PatientId==id && m.Kind=="department" && m.IsActive).ToListAsync();
        foreach (var membership in old) { membership.IsActive=false; membership.UpdatedAt=DateTime.UtcNow; membership.Reason=dto.Reason; }
        await PatientUnitMemberships.Grant(_context,id,dto.DepartmentId,"department",dto.Reason);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    private async Task<IEnumerable<PatientDto>> GetPatientsForCurrentCareUnit(CancellationToken cancellationToken)
    {
        if (!AccessRoles.IsAny(User.FindFirstValue(ClaimTypes.Role), AccessRoles.AuthenticatedCareUsers)) return [];
        var careUnitId = GetCareUnitId();
        if (string.IsNullOrWhiteSpace(careUnitId)) return [];

        var patients = await CareUnitPatientAccess.Query(_context, careUnitId, User.FindFirstValue(ClaimTypes.Role))
            .ToListAsync(cancellationToken);

        return patients.Select(DtoMapper.ToDto).ToList();
    }

    private async Task<bool> CanAccessPatient(int patientId, CancellationToken cancellationToken)
    {
        if (CanManageAllPatients()) return true;

        var careUnitId = GetCareUnitId();
        return !string.IsNullOrWhiteSpace(careUnitId) &&
               await CareUnitPatientAccess.Query(_context, careUnitId, User.FindFirstValue(ClaimTypes.Role)).AnyAsync(patient => patient.PatientId == patientId,
                   cancellationToken);
    }

    // DTO validators are not run by the MediatR pipeline (it validates commands), so patient
    // data is validated here, and the personnummer is stored in one form: ÅÅÅÅMMDD-XXXX.
    private async Task<IActionResult?> ValidateAndNormalize(CreatePatientDto dto)
    {
        var result = await Validator.ValidateAsync(dto);
        if (!result.IsValid)
        {
            return BadRequest(new
            {
                message = result.Errors[0].ErrorMessage,
                errors = result.Errors.GroupBy(e => e.PropertyName).ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray())
            });
        }

        Personnummer.TryNormalize(dto.SocialSecurityNumber, DateTime.Today, out var normalized);
        dto.SocialSecurityNumber = normalized;
        dto.FullName = dto.FullName.Trim();
        return null;
    }

    private bool CanManageAllPatients()
    {
        return AccessRoles.IsAdmin(User.FindFirstValue(ClaimTypes.Role));
    }

    private string? GetCareUnitId()
    {
        return User.FindFirstValue("care_unit");
    }
}
