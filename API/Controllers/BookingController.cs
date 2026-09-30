// Backend purpose: Exposes HTTP endpoints for the BookingController backend feature.
using BookingSystem.Application.Commands.BookingCommand.CreateBooking;
using BookingSystem.Application.Commands.BookingCommand.DeleteCommand;
using BookingSystem.Application.Commands.BookingCommand.UpdateBooking;
using BookingSystem.Application.Authorization;
using BookingSystem.Application.Common;
using BookingSystem.Application.DTO;
using BookingSystem.Application.Queries.QueriesBooking.GetAllBookings;
using BookingSystem.Application.Queries.QueriesBooking.GetBookingById;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace API.Controllers
{
    
    [ApiController]
    [Authorize]
    [Route("api/[controller]")]
    public class BookingController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly AuditLogWriter _auditLogWriter;

        public BookingController(IMediator mediator, AuditLogWriter auditLogWriter)
        {
            _mediator = mediator;
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
            if(!result.Success)
                return NotFound(result.ErrorMessage);

            if (result.Data is null || !CanAccessBooking(result.Data)) return Forbid();

            await _auditLogWriter.RecordReadAsync("Booking", id.ToString(), cancellationToken);
            return Ok(result.Data);
        }

        // POST: api/booking
        [HttpPost]
        public async Task<IActionResult> CreateBooking([FromBody] CreateBookingDto dto)
        {
            if (!CanManageAllBookings())
            {
                var careUnitId = GetCareUnitId();
                if (string.IsNullOrWhiteSpace(careUnitId)) return Forbid();

                if (!string.IsNullOrWhiteSpace(dto.ReferralDepartment) &&
                    !string.Equals(dto.ReferralDepartment, careUnitId, StringComparison.OrdinalIgnoreCase))
                {
                    return Forbid();
                }

                dto.ReferralDepartment = careUnitId;
                dto.CareUnitId = careUnitId;
            }

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
            if (!CanManageAllBookings()) return Forbid();

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
            if (!CanManageAllBookings()) return Forbid();

            var result = await _mediator.Send(new DeleteBookingCommand(id));
            if (!result) return NotFound();

            return Ok(new { message = "Booking successfully deleted" });
        }

        private IEnumerable<BookingDto> FilterBookingsForCurrentUser(IEnumerable<BookingDto> bookings)
        {
            if (CanManageAllBookings()) return bookings;

            var careUnitId = GetCareUnitId();
            if (string.IsNullOrWhiteSpace(careUnitId)) return [];

            return bookings.Where(booking =>
                string.Equals(booking.CareUnitId ?? booking.ReferralDepartment, careUnitId, StringComparison.OrdinalIgnoreCase));
        }

        private bool CanAccessBooking(BookingDto booking)
        {
            if (CanManageAllBookings()) return true;

            var careUnitId = GetCareUnitId();
            return !string.IsNullOrWhiteSpace(careUnitId) &&
                   string.Equals(booking.CareUnitId ?? booking.ReferralDepartment, careUnitId, StringComparison.OrdinalIgnoreCase);
        }

        private bool CanManageAllBookings()
        {
            return AccessRoles.IsTreatmentUnit(User.FindFirstValue(ClaimTypes.Role));
        }

        private string? GetCareUnitId()
        {
            return User.FindFirstValue("care_unit");
        }
    }
}
