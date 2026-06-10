// Backend purpose: Exposes HTTP endpoints for the StaffingController backend feature.
using BookingSystem.Application.DTO;
using BookingSystem.Domain.Entities;
using BookingSystem.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Controllers;

[ApiController]
[Route("api/staffing")]
public class StaffingController : ControllerBase
{
    private readonly AppDbContext _context;

    public StaffingController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<StaffingStatusDto>> Get([FromQuery] DateTime date, CancellationToken cancellationToken)
    {
        // Staffing is stored once per calendar day, so time components are intentionally discarded.
        var day = date.Date;
        var status = await _context.StaffingStatuses
            .AsNoTracking()
            .SingleOrDefaultAsync(s => s.Date == day, cancellationToken);

        return status == null ? NotFound() : Ok(ToDto(status));
    }

    [HttpPut]
    public async Task<ActionResult<StaffingStatusDto>> Upsert(
        [FromBody] StaffingStatusDto dto,
        CancellationToken cancellationToken)
    {
        var day = dto.Date.Date;
        // PUT behaves as an upsert so coordinators can create or revise the daily staffing record.
        var status = await _context.StaffingStatuses
            .SingleOrDefaultAsync(s => s.Date == day, cancellationToken);

        if (status == null)
        {
            status = new StaffingStatus { Date = day };
            _context.StaffingStatuses.Add(status);
        }

        // UpdatedAt is controlled by the server and represents the last persisted staffing change.
        status.AnesthesiaAvailable = dto.AnesthesiaAvailable;
        status.AnesthesiaRequired = dto.AnesthesiaRequired;
        status.NursesAvailable = dto.NursesAvailable;
        status.NursesRequired = dto.NursesRequired;
        status.DoctorsAvailable = dto.DoctorsAvailable;
        status.DoctorsRequired = dto.DoctorsRequired;
        status.BreakStart = dto.BreakStart;
        status.BreakEnd = dto.BreakEnd;
        status.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);
        return Ok(ToDto(status));
    }

    private static StaffingStatusDto ToDto(StaffingStatus status) => new()
    {
        Date = status.Date,
        AnesthesiaAvailable = status.AnesthesiaAvailable,
        AnesthesiaRequired = status.AnesthesiaRequired,
        NursesAvailable = status.NursesAvailable,
        NursesRequired = status.NursesRequired,
        DoctorsAvailable = status.DoctorsAvailable,
        DoctorsRequired = status.DoctorsRequired,
        BreakStart = status.BreakStart,
        BreakEnd = status.BreakEnd,
        UpdatedAt = status.UpdatedAt
    };
}
