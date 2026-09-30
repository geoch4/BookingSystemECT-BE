// Backend purpose: Defines the read-side application flow for GetAllPatientsQueryHandler.
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BookingSystem.Application.DTO;
using BookingSystem.Application.Mappings;
using BookingSystem.Infrastructure;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BookingSystem.Application.Queries.QueriesPatient.GetAllPatient;

public class GetAllPatientsQueryHandler : IRequestHandler<GetAllPatientsQuery, IEnumerable<PatientDto>>
{
    private readonly AppDbContext _context;

    public GetAllPatientsQueryHandler(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<PatientDto>> Handle(GetAllPatientsQuery request, CancellationToken cancellationToken)
    {
        var patients = await _context.Patients.ToListAsync(cancellationToken);
        return patients.Select(DtoMapper.ToDto).ToList();
    }
}
