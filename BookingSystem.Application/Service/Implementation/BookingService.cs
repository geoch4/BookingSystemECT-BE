// Backend purpose: Implements application service behavior for BookingService.
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BookingSystem.Application.DTO;
using BookingSystem.Application.Mappings;
using BookingSystem.Application.Services;
using BookingSystem.Domain.Entities;
using BookingSystem.Infrastructure.IRepository;

namespace BookingSystem.Application.Service.Implementation
{
    public class BookingService : IBookingService
    {
        private readonly IBookingRepository _repository;

        public BookingService(IBookingRepository repository)
        {
            _repository = repository;
        }

        public async Task<BookingDto?> GetBookingByIdAsync(int id)
        {
            var booking = await _repository.GetByIdAsync(id);
            if (booking is null) return null;

            return DtoMapper.ToDto(booking);
        }

        public async Task<IEnumerable<BookingDto>> GetAllBookingsAsync()
        {
            var bookings = await _repository.GetAllAsync();
            return bookings.Select(DtoMapper.ToDto);
        }

        public async Task<BookingDto> CreateBookingAsync(CreateBookingDto dto)
        {
            var booking = new Booking
            {
                Date = dto.Date,
                PatientId = dto.PatientId,
                TreatmentType = dto.TreatmentType,
                CreatedByUserId = dto.CreatedById,
                CreatedAt = dto.CreatedAt,
                Priority = dto.Priority,
                Status = dto.Status,
                DepartmentPriority = dto.DepartmentPriority,
                ReferralDepartment = dto.ReferralDepartment,
                ReferralContactName = dto.ReferralContactName,
                ReferralContactPhone = dto.ReferralContactPhone,
                CancellationReason = dto.CancellationReason,
                ExternalPatientId = dto.ExternalPatientId,
                SourceSystem = DtoMapper.CleanSourceSystem(dto.SourceSystem),
                CareUnitId = dto.CareUnitId ?? dto.ReferralDepartment,
                RegionCode = dto.RegionCode,
                IntegrationStatus = DtoMapper.ParseIntegrationStatus(dto.IntegrationStatus)
            };

            await _repository.AddAsync(booking);
            return DtoMapper.ToDto(booking);
        }

        public async Task<bool> UpdateBookingAsync(int id, CreateBookingDto dto)
        {
            var booking = await _repository.GetByIdAsync(id);
            if (booking is null) return false;

            booking.Date = dto.Date;
            booking.PatientId = dto.PatientId;
            booking.TreatmentType = dto.TreatmentType;
            booking.CreatedByUserId = dto.CreatedById;
            booking.CreatedAt = dto.CreatedAt;
            booking.Priority = dto.Priority;
            booking.Status = dto.Status;
            booking.DepartmentPriority = dto.DepartmentPriority;
            booking.ReferralDepartment = dto.ReferralDepartment;
            booking.ReferralContactName = dto.ReferralContactName;
            booking.ReferralContactPhone = dto.ReferralContactPhone;
            booking.CancellationReason = dto.CancellationReason;
            booking.ExternalPatientId = dto.ExternalPatientId;
            booking.SourceSystem = DtoMapper.CleanSourceSystem(dto.SourceSystem);
            booking.CareUnitId = dto.CareUnitId ?? dto.ReferralDepartment;
            booking.RegionCode = dto.RegionCode;
            booking.IntegrationStatus = DtoMapper.ParseIntegrationStatus(dto.IntegrationStatus);

            await _repository.UpdateAsync(booking);
            return true;
        }

        public async Task<bool> DeleteBookingAsync(int id)
        {
            var booking = await _repository.GetByIdAsync(id);
            if (booking is null) return false;

            await _repository.DeleteAsync(booking);
            return true;
        }
    }
}
