// Backend purpose: Defines the read-side application flow for GetAllBookingsQueryHandler.
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BookingSystem.Application.Common;
using BookingSystem.Application.DTO;
using BookingSystem.Application.Mappings;
using BookingSystem.Infrastructure;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BookingSystem.Application.Queries.QueriesBooking.GetAllBookings;

public class GetAllBookingsQueryHandler : IRequestHandler<GetAllBookingsQuery, OperationResult<List<BookingDto>>>
{
    private readonly AppDbContext _context;

    public GetAllBookingsQueryHandler(AppDbContext context)
    {
        _context = context;
    }

    public async Task<OperationResult<List<BookingDto>>> Handle(GetAllBookingsQuery request, CancellationToken cancellationToken)
    {
        var bookings = await _context.Bookings.ToListAsync(cancellationToken);
        
        var bookingsDto = bookings.Select(DtoMapper.ToDto).ToList();
        
        return OperationResult<List<BookingDto>>.Ok(bookingsDto);
    }
}
