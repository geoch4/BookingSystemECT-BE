using BookingSystem.Application.Common;
using BookingSystem.Application.DTO;
using BookingSystem.Application.Authorization;
using BookingSystem.Application.Mappings;
using BookingSystem.Domain.Entities;
using BookingSystem.Domain.Enums;
using BookingSystem.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace API.Controllers;

[Authorize(Policy = AccessPolicies.CareStaff)]
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

        var referrals = await ScopedBookings()
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
                DepartmentPriority = b.DepartmentPriority,
                Status = b.Status == BookingStatus.Confirmed
                    ? ReferralStatus.Scheduled
                    : b.Status == BookingStatus.Cancelled
                        ? ReferralStatus.Cancelled
                        : ReferralStatus.Submitted,
                RequestedStartDate = b.RequestedStartDate ?? b.Date,
                CreatedAt = b.CreatedAt,
                ExternalPatientId = b.ExternalPatientId,
                SourceSystem = b.SourceSystem,
                CareUnitId = b.CareUnitId,
                DepartmentId = b.DepartmentId,
                TreatmentUnitId = b.TreatmentUnitId,
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
        if (!AccessRoles.IsDepartmentUser(GetRole()) && !AccessRoles.IsAdmin(GetRole())) return Forbid();
        var query = ScopedBookings();

        if (!CanManageReferralQueue())
        {
            if (string.IsNullOrWhiteSpace(careUnitId)) return Forbid();
            query = query.Where(b => (b.DepartmentId ?? b.CareUnitId ?? b.ReferralDepartment) == careUnitId);
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
                DepartmentPriority = b.DepartmentPriority,
                Status = b.Status == BookingStatus.Confirmed
                    ? ReferralStatus.Scheduled
                    : b.Status == BookingStatus.Cancelled
                        ? ReferralStatus.Cancelled
                        : ReferralStatus.Submitted,
                RequestedStartDate = b.RequestedStartDate ?? b.Date,
                CreatedAt = b.CreatedAt,
                ExternalPatientId = b.ExternalPatientId,
                SourceSystem = b.SourceSystem,
                CareUnitId = b.CareUnitId,
                DepartmentId = b.DepartmentId,
                TreatmentUnitId = b.TreatmentUnitId,
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

    // The clinical basis is health information: served per referral, behind the same access
    // check as the referral itself, and every read is written to the audit log.
    [HttpGet("{referralId}/clinical")]
    public async Task<ActionResult<ReferralClinicalDetailsDto>> GetClinicalDetails(string referralId, CancellationToken cancellationToken)
    {
        var id = ParseReferralId(referralId);
        if (id is null) return BadRequest(new { message = "Ogiltigt remiss-id." });

        var booking = await _context.Bookings.FirstOrDefaultAsync(b => b.BookingId == id.Value, cancellationToken);
        if (booking is null) return NotFound(new { message = "Remissen hittades inte." });
        if (!CanViewReferral(booking)) return Forbid();

        var details = await _context.ReferralClinicalDetails
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.BookingId == id.Value, cancellationToken);

        await _auditLogWriter.RecordReadAsync("ReferralClinicalDetails", id.Value.ToString(), cancellationToken);
        if (details is null) return NoContent();
        return Ok(DtoMapper.ToDto(details));
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
        if (!AccessRoles.IsAdmin(GetRole()) && !AccessRoles.IsMedicalResponsible(GetRole())) return Forbid();

        var id = ParseReferralId(referralId);
        if (id is null) return BadRequest(new { message = "Ogiltigt remiss-id." });

        var booking = await _context.Bookings.FirstOrDefaultAsync(b => b.BookingId == id.Value, cancellationToken);
        if (booking is null) return NotFound(new { message = "Remissen hittades inte." });

        if (!CanViewReferral(booking)) return Forbid();
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
        if (!CanViewReferral(booking)) return Forbid();
        if (dto.ConfirmedDateTime is null) return BadRequest(new { message = "Bekräftad tid saknas." });

        if (dto.Priority is not null && dto.Priority.Value != booking.Priority && !AccessRoles.IsAdmin(GetRole()) && !AccessRoles.IsMedicalResponsible(GetRole())) return Forbid();
        var confirmedDate = BookingSlots.NormalizeToMinute(dto.ConfirmedDateTime.Value);
        if (await BookingSlots.IsOccupiedAsync(_context, booking.TreatmentUnitId, confirmedDate, booking.BookingId, cancellationToken))
        {
            return Conflict(new { message = BookingErrors.SlotOccupied });
        }

        if (dto.Priority is not null) booking.Priority = dto.Priority.Value;
        booking.Date = confirmedDate;
        booking.Status = BookingStatus.Confirmed;

        try
        {
            return await CreateAction(id.Value, dto, ReferralActionType.TimeConfirmed, "Tid bekräftad", cancellationToken);
        }
        catch (DbUpdateException ex) when (BookingSlots.IsUniqueConstraintViolation(ex))
        {
            return Conflict(new { message = BookingErrors.SlotOccupied });
        }
    }

    [HttpPatch("{referralId}/department-priority")]
    public async Task<ActionResult<ReferralDto>> UpdateDepartmentPriority(
        string referralId,
        [FromBody] UpdateDepartmentPriorityDto dto,
        CancellationToken cancellationToken)
    {
        var id = ParseReferralId(referralId);
        if (id is null) return BadRequest(new { message = "Ogiltigt remiss-id." });

        if (dto.DepartmentPriority is < 1 or > 3)
            return BadRequest(new { message = "Avdelningsprioritet måste vara 1, 2 eller 3." });

        var booking = await _context.Bookings.FirstOrDefaultAsync(b => b.BookingId == id.Value, cancellationToken);
        if (booking is null) return NotFound(new { message = "Remissen hittades inte." });
        if (!CanSubmitDepartmentAction(booking)) return Forbid();

        booking.DepartmentPriority = dto.DepartmentPriority;
        await _context.SaveChangesAsync(cancellationToken);

        return Ok(new ReferralDto
        {
            ReferralId = booking.BookingId,
            BookingId = booking.BookingId,
            PatientId = booking.PatientId,
            TreatmentTypeId = booking.TreatmentType,
            Department = booking.ReferralDepartment ?? string.Empty,
            ContactName = booking.ReferralContactName,
            ContactPhone = booking.ReferralContactPhone,
            Priority = booking.Priority,
            DepartmentPriority = booking.DepartmentPriority,
            Status = booking.Status == BookingStatus.Confirmed
                ? ReferralStatus.Scheduled
                : booking.Status == BookingStatus.Cancelled
                    ? ReferralStatus.Cancelled
                    : ReferralStatus.Submitted,
            RequestedStartDate = booking.RequestedStartDate ?? booking.Date,
            CreatedAt = booking.CreatedAt,
            ExternalPatientId = booking.ExternalPatientId,
            SourceSystem = booking.SourceSystem,
            CareUnitId = booking.CareUnitId,
            DepartmentId = booking.DepartmentId,
            TreatmentUnitId = booking.TreatmentUnitId,
            RegionCode = booking.RegionCode,
            IntegrationStatus = booking.IntegrationStatus.ToString()
        });
    }

    [HttpPost("{referralId}/actions/patient-action")]
    public async Task<ActionResult<ReferralActionDto>> RecordPatientAction(
        string referralId,
        [FromBody] CreateReferralActionDto dto,
        CancellationToken cancellationToken)
    {
        var id = ParseReferralId(referralId);
        if (id is null) return BadRequest(new { message = "Ogiltigt remiss-id." });

        if (dto.ActionType is not (ReferralActionType.PatientPaused or ReferralActionType.PatientCancelled or ReferralActionType.PatientEnded))
            return BadRequest(new { message = "Ogiltig patientåtgärd." });

        if (string.IsNullOrWhiteSpace(dto.Comment))
            return BadRequest(new { message = "Kommentar/orsak krävs för patientåtgärden." });

        var booking = await _context.Bookings.FirstOrDefaultAsync(b => b.BookingId == id.Value, cancellationToken);
        if (booking is null) return NotFound(new { message = "Remissen hittades inte." });
        if (!CanRecordPatientAction(booking)) return Forbid();

        if (dto.ActionType == ReferralActionType.PatientCancelled)
        {
            booking.Status = BookingStatus.Cancelled;
            booking.CancellationReason = dto.Comment.Trim();
        }

        return await CreateAction(id.Value, dto, dto.ActionType, PatientActionTitle(dto.ActionType), cancellationToken);
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
        if (!CanSubmitDepartmentAction(booking)) return Forbid();

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

        if (!CanViewReferral(booking)) return Forbid();
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var actorId)) return Forbid();
        var action = new ReferralAction
        {
            ReferralId = referralId,
            ActionType = actionType,
            Title = title,
            Comment = dto.Comment,
            Priority = dto.Priority,
            ConfirmedDateTime = dto.ConfirmedDateTime is null ? null : BookingSlots.NormalizeToMinute(dto.ConfirmedDateTime.Value),
            CreatedById = actorId,
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
            ReferralActionType.PatientPaused => "warning",
            ReferralActionType.PatientCancelled => "danger",
            ReferralActionType.PatientEnded => "info",
            _ => "info"
        };
    }

    private static string PatientActionTitle(ReferralActionType actionType)
    {
        return actionType switch
        {
            ReferralActionType.PatientPaused => "Behandling pausad",
            ReferralActionType.PatientCancelled => "Tid avbokad",
            ReferralActionType.PatientEnded => "Behandling avslutad",
            _ => "Patientåtgärd"
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
                .Where(user => AccessRoles.IsDepartmentUser(user.Role?.RoleName) && !string.IsNullOrWhiteSpace(booking.DepartmentId ?? booking.CareUnitId ?? booking.ReferralDepartment) &&
                               string.Equals(user.CareUnitId, booking.DepartmentId ?? booking.CareUnitId ?? booking.ReferralDepartment, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        if (actionType is ReferralActionType.CompletionSubmitted or ReferralActionType.PatientPaused or ReferralActionType.PatientCancelled or ReferralActionType.PatientEnded)
        {
            return users
                .Where(user => AccessRoles.IsAdmin(user.Role?.RoleName) || (!string.IsNullOrWhiteSpace(booking.TreatmentUnitId) && AccessRoles.IsReferralDecisionUser(user.Role?.RoleName) && string.Equals(user.CareUnitId, booking.TreatmentUnitId, StringComparison.OrdinalIgnoreCase)))
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
            ReferralActionType.PatientPaused => $"Behandling för remiss #{booking.BookingId} från {department} har pausats.",
            ReferralActionType.PatientCancelled => $"Behandlingstid för remiss #{booking.BookingId} från {department} har avbokats.",
            ReferralActionType.PatientEnded => $"Behandling för remiss #{booking.BookingId} från {department} har avslutats.",
            _ => $"Remiss #{booking.BookingId} från {department} har uppdaterats."
        };
    }

    private IQueryable<Booking> ScopedBookings() => CareUnitPatientAccess.RestrictBookings(_context,BookingAccess.Query(_context.Bookings, GetRole(), GetCareUnitId()),GetRole(),GetCareUnitId());
    private bool CanViewReferral(Booking booking) => BookingAccess.CanRead(GetRole(), GetCareUnitId(), booking.DepartmentId ?? booking.CareUnitId ?? booking.ReferralDepartment, booking.TreatmentUnitId) && (AccessRoles.IsAdmin(GetRole()) || CareUnitPatientAccess.Query(_context,GetCareUnitId() ?? "",GetRole()).Any(p=>p.PatientId==booking.PatientId));

    private bool CanSubmitDepartmentAction(Booking booking)
    {
        return AccessRoles.IsDepartmentUser(GetRole()) && IsOwnCareUnitReferral(booking) && CanViewReferral(booking);
    }

    private bool CanRecordPatientAction(Booking booking)
    {
        return AccessRoles.IsDepartmentUser(GetRole()) && IsOwnCareUnitReferral(booking) && CanViewReferral(booking);
    }

    private bool IsOwnCareUnitReferral(Booking booking)
    {
        var careUnitId = GetCareUnitId();
        return !string.IsNullOrWhiteSpace(careUnitId) &&
               string.Equals(booking.DepartmentId ?? booking.CareUnitId ?? booking.ReferralDepartment, careUnitId, StringComparison.OrdinalIgnoreCase);
    }

    private bool CanManageReferralQueue()
    {
        var role = GetRole();
        return AccessRoles.IsReferralDecisionUser(role);
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
