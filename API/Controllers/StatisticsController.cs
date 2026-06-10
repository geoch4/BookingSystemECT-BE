// Backend purpose: Exposes HTTP endpoints for the StatisticsController backend feature.
using BookingSystem.Application.DTO;
using BookingSystem.Domain.Enums;
using BookingSystem.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Controllers;

[ApiController]
[Route("api/statistics")]
public class StatisticsController : ControllerBase
{
    private readonly AppDbContext _context;

    public StatisticsController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet("bookings")]
    public async Task<ActionResult<BookingStatisticsDto>> GetBookings(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        CancellationToken cancellationToken)
    {
        // Default to a rolling 30-day window and use an exclusive upper boundary for stable date filtering.
        var end = (to ?? DateTime.UtcNow).Date.AddDays(1);
        var start = (from ?? end.AddDays(-30)).Date;
        if (start >= end) return BadRequest(new { message = "from must be before to" });

        // Load only the fields required for aggregation to avoid materializing complete booking entities.
        var bookings = await _context.Bookings
            .AsNoTracking()
            .Where(b => b.Date >= start && b.Date < end)
            .Select(b => new
            {
                b.Status,
                b.Priority,
                b.TreatmentType,
                b.CreatedAt,
                b.Date,
                b.HealthDeclarationStatus,
                b.AnesthesiaAssessmentStatus
            })
            .ToListAsync(cancellationToken);

        // Every metric below is derived from persisted booking data; no dashboard sample values are used.
        return Ok(new BookingStatisticsDto
        {
            From = start,
            To = end.AddTicks(-1),
            Total = bookings.Count,
            Confirmed = bookings.Count(b => b.Status == BookingStatus.Confirmed),
            Pending = bookings.Count(b => b.Status == BookingStatus.Pending),
            Cancelled = bookings.Count(b => b.Status == BookingStatus.Cancelled),
            Urgent = bookings.Count(b => b.Priority == BookingPriority.Red),
            Ect = bookings.Count(b => b.TreatmentType <= TreatmentType.EctIndexserie),
            Rtms = bookings.Count(b => b.TreatmentType >= TreatmentType.RtmsDepression),
            AverageLeadTimeDays = bookings.Count == 0
                ? 0
                : Math.Round(bookings.Average(b => (b.Date - b.CreatedAt).TotalDays), 1),
            MissingHealthDeclaration = bookings.Count(b =>
                b.HealthDeclarationStatus != AssessmentStatus.Approved),
            MissingAnesthesiaAssessment = bookings.Count(b =>
                b.AnesthesiaAssessmentStatus != AssessmentStatus.Approved)
        });
    }
}
