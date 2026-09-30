using BookingSystem.Application.Common;
using BookingSystem.Application.DTO;
using BookingSystem.Application.Authorization;
using BookingSystem.Domain.Entities;
using BookingSystem.Domain.Enums;
using BookingSystem.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace API.Controllers;

[Authorize]
[ApiController]
[Route("api/referrals")]
public class ReferralsController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly AuditLogWriter _auditLogWriter;

    public ReferralsController(AppDbContext context, AuditLogWriter auditLogWriter)
    {
        _context = context;
        _auditLogWriter = auditLogWriter;
    }

    [HttpGet("queue")]
    public async Task<ActionResult<IEnumerable<ReferralDto>>> GetQueue(CancellationToken cancellationToken)
    {
        if (!CanManageReferralQueue()) return Forbid();

        var referrals = await _context.Bookings
            .OrderBy(b => b.Date)
            .Select(b => new ReferralDto
            {
                ReferralId = b.BookingId,
                BookingId = b.BookingId,
                PatientId = b.PatientId,
                TreatmentTypeId = b.TreatmentType,
                Department = b.ReferralDepartment ?? string.Empty,
                ContactName = b.ReferralContactName,
                ContactPhone = b.ReferralContactPhone,
                Priority = b.Priority,
                Status = b.Status == BookingStatus.Confirmed
                    ? ReferralStatus.Scheduled
                    : b.Status == BookingStatus.Cancelled
                        ? ReferralStatus.Cancelled
                        : ReferralStatus.Submitted,
                RequestedStartDate = b.Date,
                CreatedAt = b.CreatedAt,
                ExternalPatientId = b.ExternalPatientId,
                SourceSystem = b.SourceSystem,
                CareUnitId = b.CareUnitId,
                RegionCode = b.RegionCode,
                IntegrationStatus = b.IntegrationStatus.ToString()
            })
            .ToListAsync(cancellationToken);

        await _auditLogWriter.RecordReadAsync("Referral", "queue", cancellationToken);
        return Ok(referrals);
    }

    [HttpGet("my")]
    public async Task<ActionResult<IEnumerable<ReferralDto>>> GetMyReferrals(CancellationToken cancellationToken)
    {
        var careUnitId = GetCareUnitId();
        var query = _context.Bookings.AsQueryable();

        if (!CanManageReferralQueue())
        {
            if (string.IsNullOrWhiteSpace(careUnitId)) return Forbid();
            query = query.Where(b => (b.CareUnitId ?? b.ReferralDepartment) == careUnitId);
        }

        var referrals = await query
            .OrderByDescending(b => b.CreatedAt)
            .Select(b => new ReferralDto
            {
                ReferralId = b.BookingId,
                BookingId = b.BookingId,
                PatientId = b.PatientId,
                TreatmentTypeId = b.TreatmentType,
                Department = b.ReferralDepartment ?? string.Empty,
                ContactName = b.ReferralContactName,
                ContactPhone = b.ReferralContactPhone,
                Priority = b.Priority,
                Status = b.Status == BookingStatus.Confirmed
                    ? ReferralStatus.Scheduled
                    : b.Status == BookingStatus.Cancelled
                        ? ReferralStatus.Cancelled
                        : ReferralStatus.Submitted,
                RequestedStartDate = b.Date,
                CreatedAt = b.CreatedAt,
                ExternalPatientId = b.ExternalPatientId,
                SourceSystem = b.SourceSystem,
                CareUnitId = b.CareUnitId,
                RegionCode = b.RegionCode,
                IntegrationStatus = b.IntegrationStatus.ToString()
            })
            .ToListAsync(cancellationToken);

        await _auditLogWriter.RecordReadAsync("Referral", "my", cancellationToken);
        return Ok(referrals);
    }

    [HttpGet("{referralId}/actions")]
    public async Task<ActionResult<IEnumerable<ReferralActionDto>>> GetActions(string referralId, CancellationToken cancellationToken)
    {
        var id = ParseReferralId(referralId);
        if (id is null) return BadRequest(new { message = "Ogiltigt remiss-id." });

        var booking = await _context.Bookings.FirstOrDefaultAsync(b => b.BookingId == id.Value, cancellationToken);
        if (booking is null) return NotFound(new { message = "Remissen hittades inte." });
        if (!CanViewReferral(booking)) return Forbid();

        var actions = await _context.ReferralActions
            .Where(a => a.ReferralId == id.Value)
            .OrderBy(a => a.CreatedAt)
            .Select(a => ToActionDto(a))
            .ToListAsync(cancellationToken);

        await _auditLogWriter.RecordReadAsync("Referral", id.Value.ToString(), cancellationToken);
        return Ok(actions);
    }

    [HttpGet("{referralId}/timeline")]
    public async Task<ActionResult<IEnumerable<ReferralTimelineEventDto>>> GetTimeline(string referralId, CancellationToken cancellationToken)
    {
        var id = ParseReferralId(referralId);
        if (id is null) return BadRequest(new { message = "Ogiltigt remiss-id." });

        var booking = await _context.Bookings.FirstOrDefaultAsync(b => b.BookingId == id.Value, cancellationToken);
        if (booking is null) return NotFound(new { message = "Remissen hittades inte." });
        if (!CanViewReferral(booking)) return Forbid();

        var actions = await _context.ReferralActions
            .Where(a => a.ReferralId == id.Value)
            .OrderBy(a => a.CreatedAt)
            .ToListAsync(cancellationToken);

        var events = new List<ReferralTimelineEventDto>
        {
            new()
            {
                Title = "Remiss skapad",
                Detail = $"Remiss från {booking.ReferralDepartment ?? "okänd avdelning"} registrerades.",
                CreatedAt = booking.CreatedAt,
                Tone = "info"
            }
        };

        events.AddRange(actions.Select(a => new ReferralTimelineEventDto
        {
            Title = a.Title,
            Detail = a.Comment,
            CreatedAt = a.CreatedAt,
            Tone = ToneFor(a.ActionType)
        }));

        await _auditLogWriter.RecordReadAsync("Referral", id.Value.ToString(), cancellationToken);
        return Ok(events.OrderBy(e => e.CreatedAt));
    }

    [HttpPost("{referralId}/actions/completion-request")]
    public Task<ActionResult<ReferralActionDto>> RequestCompletion(
        string referralId,
        [FromBody] CreateReferralActionDto dto,
        CancellationToken cancellationToken)
    {
        if (!CanManageReferralQueue()) return Task.FromResult<ActionResult<ReferralActionDto>>(Forbid());
        return CreateAction(referralId, dto, ReferralActionType.CompletionRequested, "Komplettering begärd", cancellationToken);
    }

    [HttpPost("{referralId}/actions/completion-response")]
    public Task<ActionResult<ReferralActionDto>> SubmitCompletion(
        string referralId,
        [FromBody] CreateReferralActionDto dto,
        CancellationToken cancellationToken)
    {
        return CreateDepartmentAction(referralId, dto, ReferralActionType.CompletionSubmitted, "Komplettering skickad från avdelningen", cancellationToken);
    }

    [HttpPost("{referralId}/actions/prioritize")]
    public async Task<ActionResult<ReferralActionDto>> Prioritize(
        string referralId,
        [FromBody] CreateReferralActionDto dto,
        CancellationToken cancellationToken)
    {
        if (!CanManageReferralQueue()) return Forbid();

        var id = ParseReferralId(referralId);
        if (id is null) return BadRequest(new { message = "Ogiltigt remiss-id." });

        var booking = await _context.Bookings.FirstOrDefaultAsync(b => b.BookingId == id.Value, cancellationToken);
        if (booking is null) return NotFound(new { message = "Remissen hittades inte." });

        if (dto.Priority is not null) booking.Priority = dto.Priority.Value;

        return await CreateAction(id.Value, dto, ReferralActionType.Prioritized, "Prioritet bedömd", cancellationToken);
    }

    [HttpPost("{referralId}/actions/confirm-time")]
    public async Task<ActionResult<ReferralActionDto>> ConfirmTime(
        string referralId,
        [FromBody] CreateReferralActionDto dto,
        CancellationToken cancellationToken)
    {
        if (!CanManageReferralQueue()) return Forbid();

        var id = ParseReferralId(referralId);
        if (id is null) return BadRequest(new { message = "Ogiltigt remiss-id." });

        var booking = await _context.Bookings.FirstOrDefaultAsync(b => b.BookingId == id.Value, cancellationToken);
        if (booking is null) return NotFound(new { message = "Remissen hittades inte." });
        if (dto.ConfirmedDateTime is null) return BadRequest(new { message = "Bekräftad tid saknas." });

        var confirmedDate = NormalizeToMinute(dto.ConfirmedDateTime.Value);
        if (await _context.Bookings.AnyAsync(
                b => b.BookingId != booking.BookingId &&
                     b.Date == confirmedDate &&
                     b.Status != BookingStatus.Cancelled,
                cancellationToken))
        {
            return Conflict(new { message = BookingErrors.SlotOccupied });
        }

        booking.Date = confirmedDate;
        booking.Status = BookingStatus.Confirmed;

        try
        {
            return await CreateAction(id.Value, dto, ReferralActionType.TimeConfirmed, "Tid bekräftad", cancellationToken);
        }
        catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
        {
            return Conflict(new { message = BookingErrors.SlotOccupied });
        }
    }

    private async Task<ActionResult<ReferralActionDto>> CreateAction(
        string referralId,
        CreateReferralActionDto dto,
        ReferralActionType actionType,
        string title,
        CancellationToken cancellationToken)
    {
        var id = ParseReferralId(referralId);
        if (id is null) return BadRequest(new { message = "Ogiltigt remiss-id." });

        return await CreateAction(id.Value, dto, actionType, title, cancellationToken);
    }

    private async Task<ActionResult<ReferralActionDto>> CreateDepartmentAction(
        string referralId,
        CreateReferralActionDto dto,
        ReferralActionType actionType,
        string title,
        CancellationToken cancellationToken)
    {
        var id = ParseReferralId(referralId);
        if (id is null) return BadRequest(new { message = "Ogiltigt remiss-id." });

        var booking = await _context.Bookings.FirstOrDefaultAsync(b => b.BookingId == id.Value, cancellationToken);
        if (booking is null) return NotFound(new { message = "Remissen hittades inte." });
        if (!CanViewReferral(booking)) return Forbid();

        return await CreateAction(id.Value, dto, actionType, title, cancellationToken);
    }

    private async Task<ActionResult<ReferralActionDto>> CreateAction(
        int referralId,
        CreateReferralActionDto dto,
        ReferralActionType actionType,
        string title,
        CancellationToken cancellationToken)
    {
        var booking = await _context.Bookings.FirstOrDefaultAsync(b => b.BookingId == referralId, cancellationToken);
        if (booking is null) return NotFound(new { message = "Remissen hittades inte." });

        var action = new ReferralAction
        {
            ReferralId = referralId,
            ActionType = actionType,
            Title = title,
            Comment = dto.Comment,
            Priority = dto.Priority,
            ConfirmedDateTime = dto.ConfirmedDateTime is null ? null : NormalizeToMinute(dto.ConfirmedDateTime.Value),
            CreatedById = dto.CreatedById,
            CreatedAt = DateTime.UtcNow
        };

        _context.ReferralActions.Add(action);
        await AddReferralNotifications(booking, actionType, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        return Ok(ToActionDto(action));
    }

    private static ReferralActionDto ToActionDto(ReferralAction action)
    {
        return new ReferralActionDto
        {
            ReferralActionId = action.ReferralActionId,
            ReferralId = action.ReferralId,
            ActionType = action.ActionType,
            Title = action.Title,
            Comment = action.Comment,
            Priority = action.Priority,
            ConfirmedDateTime = action.ConfirmedDateTime,
            CreatedAt = action.CreatedAt,
            CreatedById = action.CreatedById
        };
    }

    private static string ToneFor(ReferralActionType actionType)
    {
        return actionType switch
        {
            ReferralActionType.CompletionRequested => "warning",
            ReferralActionType.CompletionSubmitted => "info",
            ReferralActionType.Prioritized => "success",
            ReferralActionType.TimeConfirmed => "success",
            _ => "info"
        };
    }

    private static int? ParseReferralId(string referralId)
    {
        if (int.TryParse(referralId, out var numericId)) return numericId;

        const string prefix = "referral-";
        if (referralId.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) &&
            int.TryParse(referralId[prefix.Length..], out var prefixedId))
        {
            return prefixedId;
        }

        return null;
    }

    private static DateTime NormalizeToMinute(DateTime date)
    {
        return new DateTime(date.Year, date.Month, date.Day, date.Hour, date.Minute, 0, date.Kind);
    }

    private static bool IsUniqueConstraintViolation(DbUpdateException exception)
    {
        return exception.InnerException is SqlException { Number: 2601 or 2627 };
    }

    private async Task AddReferralNotifications(Booking booking, ReferralActionType actionType, CancellationToken cancellationToken)
    {
        var recipients = await GetNotificationRecipients(booking, actionType, cancellationToken);
        if (recipients.Count == 0) return;

        var message = NotificationMessage(booking, actionType);
        var now = DateTime.UtcNow;

        _context.Notifications.AddRange(recipients.Select(user => new Notification
        {
            Message = message,
            RecipientId = user.UserId,
            SentAt = now,
            IsRead = false
        }));
    }

    private async Task<List<User>> GetNotificationRecipients(
        Booking booking,
        ReferralActionType actionType,
        CancellationToken cancellationToken)
    {
        var users = await _context.Users
            .Include(u => u.Role)
            .ToListAsync(cancellationToken);

        if (actionType is ReferralActionType.CompletionRequested or ReferralActionType.Prioritized or ReferralActionType.TimeConfirmed)
        {
            return users
                .Where(user => !string.IsNullOrWhiteSpace(booking.CareUnitId ?? booking.ReferralDepartment) &&
                               string.Equals(user.CareUnitId, booking.CareUnitId ?? booking.ReferralDepartment, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        if (actionType == ReferralActionType.CompletionSubmitted)
        {
            return users
                .Where(user => AccessRoles.IsTreatmentUnit(user.Role?.RoleName))
                .ToList();
        }

        return [];
    }

    private static string NotificationMessage(Booking booking, ReferralActionType actionType)
    {
        var department = string.IsNullOrWhiteSpace(booking.ReferralDepartment)
            ? "en avdelning"
            : booking.ReferralDepartment;

        return actionType switch
        {
            ReferralActionType.CompletionRequested => $"Remiss #{booking.BookingId} från {department} behöver kompletteras.",
            ReferralActionType.CompletionSubmitted => $"Remiss #{booking.BookingId} från {department} har kompletterats.",
            ReferralActionType.Prioritized => $"Remiss #{booking.BookingId} från {department} har prioriterats.",
            ReferralActionType.TimeConfirmed => $"Remiss #{booking.BookingId} från {department} har fått bekräftad behandlingstid.",
            _ => $"Remiss #{booking.BookingId} från {department} har uppdaterats."
        };
    }

    private bool CanViewReferral(Booking booking)
    {
        if (CanManageReferralQueue()) return true;

        var careUnitId = GetCareUnitId();
        return !string.IsNullOrWhiteSpace(careUnitId) &&
               string.Equals(booking.CareUnitId ?? booking.ReferralDepartment, careUnitId, StringComparison.OrdinalIgnoreCase);
    }

    private bool CanManageReferralQueue()
    {
        var role = GetRole();
        return AccessRoles.IsTreatmentUnit(role);
    }

    private string? GetRole()
    {
        return User.FindFirstValue(ClaimTypes.Role);
    }

    private string? GetCareUnitId()
    {
        return User.FindFirstValue("care_unit");
    }

}
