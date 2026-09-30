// Backend purpose: Exposes HTTP endpoints for the PatientController backend feature.
using BookingSystem.Application.Commands.PatientCommand.CreatePatient;
using BookingSystem.Application.Commands.PatientCommand.DeletePatient;
using BookingSystem.Application.Commands.PatientCommand.UpdatePatient;
using BookingSystem.Application.Authorization;
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
[Authorize]
[Route("api/[controller]")]
public class PatientsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly AuditLogWriter _auditLogWriter;
    private readonly AppDbContext _context;

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
        var created = await _mediator.Send(new CreatePatientCommand(dto));
        return CreatedAtAction(nameof(GetById), new { id = created.PatientId }, created);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] CreatePatientDto dto)
    {
        if (!CanManageAllPatients()) return Forbid();

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

    private async Task<IEnumerable<PatientDto>> GetPatientsForCurrentCareUnit(CancellationToken cancellationToken)
    {
        var careUnitId = GetCareUnitId();
        if (string.IsNullOrWhiteSpace(careUnitId)) return [];

        var patients = await _context.Patients
            .Where(patient => _context.Bookings.Any(booking =>
                booking.PatientId == patient.PatientId &&
                (booking.CareUnitId ?? booking.ReferralDepartment) == careUnitId))
            .ToListAsync(cancellationToken);

        return patients.Select(DtoMapper.ToDto).ToList();
    }

    private async Task<bool> CanAccessPatient(int patientId, CancellationToken cancellationToken)
    {
        if (CanManageAllPatients()) return true;

        var careUnitId = GetCareUnitId();
        return !string.IsNullOrWhiteSpace(careUnitId) &&
               await _context.Bookings.AnyAsync(booking =>
                   booking.PatientId == patientId &&
                   (booking.CareUnitId ?? booking.ReferralDepartment) == careUnitId,
                   cancellationToken);
    }

    private bool CanManageAllPatients()
    {
        return AccessRoles.IsTreatmentUnit(User.FindFirstValue(ClaimTypes.Role));
    }

    private string? GetCareUnitId()
    {
        return User.FindFirstValue("care_unit");
    }
}
