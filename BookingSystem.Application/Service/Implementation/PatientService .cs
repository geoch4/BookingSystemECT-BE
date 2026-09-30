// Backend purpose: Implements application service behavior for PatientService.
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BookingSystem.Application.DTO;
using BookingSystem.Application.Mappings;
using BookingSystem.Application.Service.Interface;
using BookingSystem.Domain.Entities;
using BookingSystem.Infrastructure.IRepository;

namespace BookingSystem.Application.Service.Implementation
{
    public class PatientService : IPatientService
    {
        private readonly IPatientRepository _repository;

        public PatientService(IPatientRepository repository)
        {
            _repository = repository;
        }

        public async Task<IEnumerable<PatientDto>> GetAllAsync()
        {
            var patients = await _repository.GetAllAsync();

            return patients.Select(DtoMapper.ToDto);
        }

        public async Task<PatientDto?> GetByIdAsync(int id)
        {
            var patient = await _repository.GetByIdAsync(id);
            if (patient == null) return null;

            return DtoMapper.ToDto(patient);
        }

        public async Task<PatientDto> CreateAsync(CreatePatientDto dto)
        {
            var patient = new Patient
            {
                FullName = dto.FullName,
                SocialSecurityNumber = dto.SocialSecurityNumber,
                ExternalPatientId = dto.ExternalPatientId,
                SourceSystem = DtoMapper.CleanSourceSystem(dto.SourceSystem),
                RegionCode = dto.RegionCode,
                IntegrationStatus = DtoMapper.ParseIntegrationStatus(dto.IntegrationStatus)
            };

            await _repository.AddAsync(patient);

            return DtoMapper.ToDto(patient);
        }

        public async Task<bool> UpdateAsync(int id, CreatePatientDto dto)
        {
            var patient = await _repository.GetByIdAsync(id);
            if (patient == null) return false;

            patient.FullName = dto.FullName;
            patient.SocialSecurityNumber = dto.SocialSecurityNumber;
            patient.ExternalPatientId = dto.ExternalPatientId;
            patient.SourceSystem = DtoMapper.CleanSourceSystem(dto.SourceSystem);
            patient.RegionCode = dto.RegionCode;
            patient.IntegrationStatus = DtoMapper.ParseIntegrationStatus(dto.IntegrationStatus);

            await _repository.UpdateAsync(patient);
            return true;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var patient = await _repository.GetByIdAsync(id);
            if (patient == null) return false;

            await _repository.DeleteAsync(patient);
            return true;
        }
    }
}
