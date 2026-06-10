// Backend purpose: Exposes HTTP endpoints for the StandbyController backend feature.
using BookingSystem.Application.DTO;
using BookingSystem.Domain.Entities;
using BookingSystem.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Controllers;

[ApiController]
[Route("api/standby")]
public class StandbyController : ControllerBase
{
    private readonly AppDbContext _context;

    public StandbyController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<StandbyEntryDto>>> GetActive(CancellationToken cancellationToken)
    {
        // Active entries are ordered by clinical urgency and then waiting time.
        var entries = await _context.StandbyEntries
            .AsNoTracking()
            .Where(s => s.IsActive)
            .OrderByDescending(s => s.Urgency)
            .ThenBy(s => s.CreatedAt)
            .Select(s => new StandbyEntryDto
            {
                StandbyEntryId = s.StandbyEntryId,
                PatientId = s.PatientId,
                PatientName = s.Patient.FullName,
                Department = s.Department,
                Urgency = s.Urgency,
                CreatedAt = s.CreatedAt
            })
            .ToListAsync(cancellationToken);

        return Ok(entries);
    }

    [HttpPost]
    public async Task<ActionResult<StandbyEntryDto>> Create(
        [FromBody] CreateStandbyEntryDto dto,
        CancellationToken cancellationToken)
    {
        // A standby entry must reference a patient already known by the booking system.
        var patient = await _context.Patients.FindAsync([dto.PatientId], cancellationToken);
        if (patient == null) return BadRequest(new { message = "Patient not found" });

        // Keep one active queue position per patient while inactive entries preserve history.
        var exists = await _context.StandbyEntries
            .AnyAsync(s => s.PatientId == dto.PatientId && s.IsActive, cancellationToken);
        if (exists) return Conflict(new { message = "Patient is already on the standby list" });

        var entry = new StandbyEntry
        {
            PatientId = dto.PatientId,
            Department = dto.Department.Trim(),
            Urgency = dto.Urgency,
            CreatedAt = DateTime.UtcNow
        };

        _context.StandbyEntries.Add(entry);
        await _context.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(nameof(GetActive), new StandbyEntryDto
        {
            StandbyEntryId = entry.StandbyEntryId,
            PatientId = entry.PatientId,
            PatientName = patient.FullName,
            Department = entry.Department,
            Urgency = entry.Urgency,
            CreatedAt = entry.CreatedAt
        });
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Remove(int id, CancellationToken cancellationToken)
    {
        var entry = await _context.StandbyEntries.FindAsync([id], cancellationToken);
        if (entry == null || !entry.IsActive) return NotFound();

        // Removing an entry is a soft state change so previous queue membership is not deleted.
        entry.IsActive = false;
        await _context.SaveChangesAsync(cancellationToken);
        return NoContent();
    }
}
