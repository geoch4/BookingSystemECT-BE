// Backend purpose: Defines the read-side application flow for GetBookingByIdQueryHandler.
using System.Threading;
using System.Threading.Tasks;
using BookingSystem.Application.Common;
using BookingSystem.Application.DTO;
using BookingSystem.Infrastructure;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BookingSystem.Application.Queries.QueriesBooking.GetBookingById;

public class GetBookingByIdQueryHandler : IRequestHandler<GetBookingByIdQuery, OperationResult<BookingDto>?>
{
    private readonly AppDbContext _context;

    public GetBookingByIdQueryHandler(AppDbContext context)
    {
        _context = context;
    }

    public async Task<OperationResult<BookingDto>?> Handle(GetBookingByIdQuery request, CancellationToken cancellationToken)
    {
        var b = await _context.Bookings
            .FirstOrDefaultAsync(b => b.BookingId == request.Id, cancellationToken);

        if (b == null) 
            return OperationResult<BookingDto>.Fail("Booking not found");
        
        var dto = new BookingDto
        {
            BookingId = b.BookingId,
            Date = b.Date,
            PatientId = b.PatientId,
            TreatmentType = b.TreatmentType,
            CreatedById = b.CreatedByUserId,
            CreatedAt = b.CreatedAt,
            Priority = b.Priority,
            Status = b.Status,
            HealthDeclarationStatus = b.HealthDeclarationStatus,
            AnesthesiaAssessmentStatus = b.AnesthesiaAssessmentStatus,
            ReferralDepartment = b.ReferralDepartment,
            ReferralContactName = b.ReferralContactName,
            ReferralContactPhone = b.ReferralContactPhone
        };
        
        return OperationResult<BookingDto>.Ok(dto);
    }

   
}
