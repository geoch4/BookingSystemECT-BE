// Backend purpose: Defines the write-side application flow for CreatePatientCommandHandler.
using System.Threading;
using System.Threading.Tasks;
using BookingSystem.Application.DTO;
using BookingSystem.Application.Mappings;
using BookingSystem.Domain.Entities;
using BookingSystem.Infrastructure;
using MediatR;
using BookingSystem.Domain;
using BookingSystem.Application.Authorization;

namespace BookingSystem.Application.Commands.PatientCommand.CreatePatient;

public class CreatePatientCommandHandler : IRequestHandler<CreatePatientCommand, PatientDto>
{
    private readonly AppDbContext _context;
    private readonly IAuditContext? _auditContext;

    public CreatePatientCommandHandler(AppDbContext context, IAuditContext? auditContext = null)
    {
        _context = context;
        _auditContext = auditContext;
    }

    public async Task<PatientDto> Handle(CreatePatientCommand request, CancellationToken cancellationToken)
    {
        var patient = new Patient
        {
            FullName = request.Dto.FullName,
            SocialSecurityNumber = request.Dto.SocialSecurityNumber,
            ExternalPatientId = request.Dto.ExternalPatientId,
            SourceSystem = DtoMapper.CleanSourceSystem(request.Dto.SourceSystem),
            RegionCode = request.Dto.RegionCode,
            IntegrationStatus = DtoMapper.ParseIntegrationStatus(request.Dto.IntegrationStatus)
        };

        _context.Patients.Add(patient);
        if (AccessRoles.IsDepartmentUser(_auditContext?.Role) && !string.IsNullOrWhiteSpace(_auditContext?.CareUnitId))
            _context.PatientCareUnits.Add(new PatientCareUnit {Patient=patient,UnitId=_auditContext.CareUnitId!,Kind="department",Reason="Patient skapad av avdelningen"});
        await _context.SaveChangesAsync(cancellationToken);

        return DtoMapper.ToDto(patient);
    }
}
