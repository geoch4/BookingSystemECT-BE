// Backend purpose: Exposes HTTP endpoints for the BookingController backend feature.
using BookingSystem.Application.Commands.BookingCommand.CreateBooking;
using BookingSystem.Application.Commands.BookingCommand.DeleteCommand;
using BookingSystem.Application.Commands.BookingCommand.UpdateBooking;
using BookingSystem.Application.Authorization;
using BookingSystem.Application.Common;
using BookingSystem.Application.DTO;
using BookingSystem.Application.Validators;
using BookingSystem.Application.Queries.QueriesBooking.GetAllBookings;
using BookingSystem.Application.Queries.QueriesBooking.GetBookingById;
using MediatR;
using BookingSystem.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace API.Controllers
{
    
    [ApiController]
    [Authorize(Policy = AccessPolicies.CareStaff)]
    [Route("api/[controller]")]
    public class BookingController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly AppDbContext _context;
        private readonly AuditLogWriter _auditLogWriter;
        private static readonly ReferralClinicalDetailsDtoValidator ClinicalValidator = new();

        public BookingController(IMediator mediator, AuditLogWriter auditLogWriter, AppDbContext context)
        {
            _mediator = mediator;
            _context = context;
            _auditLogWriter = auditLogWriter;
        }

        // GET: api/booking
        [HttpGet]
        public async Task<ActionResult<IEnumerable<BookingDto>>> GetAllBookings(CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetAllBookingsQuery());
            if(!result.Success)
                return BadRequest(result.ErrorMessage);

            var bookings = FilterBookingsForCurrentUser(result.Data ?? []);
            await _auditLogWriter.RecordReadAsync("Booking", "list", cancellationToken);
            return Ok(bookings);
        }


        // GET: api/booking/5
        [HttpGet("{id}")]
        public async Task<ActionResult<BookingDto>> GetBookingById(int id, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetBookingByIdQuery(id));
            if (result is null)
                return NotFound();
            if (!result.Success)
                return NotFound(result.ErrorMessage);

            if (result.Data is null || !CanAccessBooking(result.Data)) return Forbid();

            await _auditLogWriter.RecordReadAsync("Booking", id.ToString(), cancellationToken);
            return Ok(result.Data);
        }

        // POST: api/booking
        [HttpPost]
        public async Task<IActionResult> CreateBooking([FromBody] CreateBookingDto dto)
        {
            if (!AccessRoles.IsAdmin(GetRole()) && !AccessRoles.IsDepartmentUser(GetRole())) return Forbid();
            if (dto.ClinicalDetails is not null)
            {
                // DTO validators are not run by the MediatR pipeline, so the clinical basis is checked here.
                var clinical = await ClinicalValidator.ValidateAsync(dto.ClinicalDetails);
                if (!clinical.IsValid) return BadRequest(new { message = clinical.Errors[0].ErrorMessage });
            }
            if (!AccessRoles.IsAdmin(GetRole()))
            {
                var careUnitId = GetCareUnitId();
                if (string.IsNullOrWhiteSpace(careUnitId)) return Forbid();

                if (!string.IsNullOrWhiteSpace(dto.ReferralDepartment) &&
                    !string.Equals(dto.ReferralDepartment, careUnitId, StringComparison.OrdinalIgnoreCase))
                {
                    return Forbid();
                }

                if (!await CareUnitPatientAccess.Query(_context, careUnitId).AnyAsync(patient => patient.PatientId == dto.PatientId)) return Forbid();
                dto.ReferralDepartment = careUnitId;
                dto.CareUnitId = careUnitId;
                dto.DepartmentId = careUnitId;
            }

            if (!string.IsNullOrWhiteSpace(dto.TreatmentUnitId) && !(await TreatmentUnitDirectory.GetIds(_context)).Contains(dto.TreatmentUnitId, StringComparer.OrdinalIgnoreCase)) return BadRequest(new { message = "Okänd behandlingsenhet." });
            if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var actorId)) return Forbid();
            dto.CreatedById = actorId;
            dto.CreatedAt = DateTime.UtcNow;
            if (!AccessRoles.IsAdmin(GetRole())) { dto.Status = BookingSystem.Domain.Enums.BookingStatus.Pending; dto.Priority = BookingSystem.Domain.Enums.BookingPriority.Green; }
            var result = await _mediator.Send(new CreateBookingCommand(dto));

            // A database-enforced slot collision is exposed as HTTP 409 to support concurrent clients.
            if (!result.Success)
                return Conflict(new { message = result.ErrorMessage });

            return CreatedAtAction(nameof(GetBookingById), new { id = result.Data!.BookingId }, result.Data);
        }


        // PUT: api/booking/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateBooking(int id, [FromBody] CreateBookingDto dto)
        {
            if (!AccessRoles.IsAdmin(GetRole())) return Forbid();

            var result = await _mediator.Send(new UpdateBookingCommand(id, dto));
            if (!result.Success)
            {
                // Distinguish an occupied slot from a missing booking for an accurate HTTP response.
                if (result.ErrorMessage == BookingErrors.SlotOccupied)
                    return Conflict(new { message = result.ErrorMessage });

                return NotFound(result.ErrorMessage);
            }

            return NoContent();
        }

        

        // DELETE: api/booking/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteBooking(int id)
        {
            if (!AccessRoles.IsAdmin(GetRole())) return Forbid();

            var result = await _mediator.Send(new DeleteBookingCommand(id));
            if (!result) return NotFound();

            return Ok(new { message = "Booking successfully deleted" });
        }

        [HttpPut("{id}/treatment-unit")]
        public async Task<IActionResult> AssignTreatmentUnit(int id, [FromBody] AssignTreatmentUnitDto dto)
        {
            if (!AccessRoles.IsAdmin(GetRole())) return Forbid();
            if (string.IsNullOrWhiteSpace(dto.TreatmentUnitId) || !(await TreatmentUnitDirectory.GetIds(_context)).Contains(dto.TreatmentUnitId, StringComparer.OrdinalIgnoreCase)) return BadRequest(new { message = "Välj en registrerad behandlingsenhet." });
            var booking = await _context.Bookings.FindAsync(id);
            if (booking is null) return NotFound();
            // A confirmed booking takes its minute with it to the new unit; refuse if that minute is taken there.
            if (booking.Status == BookingSystem.Domain.Enums.BookingStatus.Confirmed &&
                await BookingSlots.IsOccupiedAsync(_context, dto.TreatmentUnitId, booking.Date, booking.BookingId))
                return Conflict(new { message = BookingErrors.SlotOccupied });
            var oldUnit = booking.TreatmentUnitId;
            booking.TreatmentUnitId = dto.TreatmentUnitId;
            await PatientUnitMemberships.Grant(_context, booking.PatientId,dto.TreatmentUnitId,"treatment","Administratör tilldelade remiss");
            await PatientUnitMemberships.RevokeUnusedTreatment(_context,booking.PatientId,oldUnit);
            await _context.SaveChangesAsync();
            return NoContent();
        }
        public record AssignTreatmentUnitDto(string TreatmentUnitId);

        private IEnumerable<BookingDto> FilterBookingsForCurrentUser(IEnumerable<BookingDto> bookings) => bookings.Where(CanAccessBooking);
        private bool CanAccessBooking(BookingDto booking) => BookingAccess.CanRead(GetRole(), GetCareUnitId(), booking.DepartmentId ?? booking.CareUnitId ?? booking.ReferralDepartment, booking.TreatmentUnitId)
            && (AccessRoles.IsAdmin(GetRole()) || CareUnitPatientAccess.Query(_context, GetCareUnitId() ?? "", GetRole()).Any(p=>p.PatientId==booking.PatientId));

        private string? GetRole() => User.FindFirstValue(ClaimTypes.Role);

        private string? GetCareUnitId()
        {
            return User.FindFirstValue("care_unit");
        }
    }
}
