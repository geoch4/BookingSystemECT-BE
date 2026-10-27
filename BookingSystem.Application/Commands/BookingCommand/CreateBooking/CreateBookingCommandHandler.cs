// Backend purpose: Defines the write-side application flow for CreateBookingCommandHandler.
using System.Threading;
using System.Threading.Tasks;
using System;
using BookingSystem.Application.Common;
using BookingSystem.Application.DTO;
using BookingSystem.Application.Mappings;
using BookingSystem.Domain.Entities;
using BookingSystem.Domain.Enums;
using BookingSystem.Infrastructure;
using MediatR;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace BookingSystem.Application.Commands.BookingCommand.CreateBooking;

public class CreateBookingCommandHandler : IRequestHandler<CreateBookingCommand, OperationResult<BookingDto>>

{
    private readonly AppDbContext _context;

    public CreateBookingCommandHandler(AppDbContext context)
    {
        _context = context;
    }

    public async Task<OperationResult<BookingDto>> Handle(CreateBookingCommand request, CancellationToken cancellationToken)
    {
        var dto = request.Dto;
        var bookingDate = BookingSlots.NormalizeToMinute(dto.Date);
        var treatmentUnitId = string.IsNullOrWhiteSpace(dto.TreatmentUnitId) ? null : dto.TreatmentUnitId.Trim();

        // Only a confirmed booking takes a slot; pending referrals may share a requested day.
        if (dto.Status == BookingStatus.Confirmed &&
            await BookingSlots.IsOccupiedAsync(_context, treatmentUnitId, bookingDate, null, cancellationToken))
        {
            return OperationResult<BookingDto>.Fail(BookingErrors.SlotOccupied);
        }

        var booking = new Booking
        {
            Date = bookingDate,
            RequestedStartDate = (dto.RequestedStartDate ?? dto.Date).Date,
            PatientId = dto.PatientId,
            TreatmentType = dto.TreatmentType,
            CreatedByUserId = dto.CreatedById,
            CreatedAt = dto.CreatedAt,
            Priority = dto.Priority,
            Status = dto.Status,
            DepartmentPriority = dto.DepartmentPriority,
            HealthDeclarationStatus = dto.HealthDeclarationStatus,
            AnesthesiaAssessmentStatus = dto.AnesthesiaAssessmentStatus,
            ReferralDepartment = dto.ReferralDepartment,
            ReferralContactName = dto.ReferralContactName,
            ReferralContactPhone = dto.ReferralContactPhone,
            CancellationReason = dto.Status == BookingStatus.Cancelled ? dto.CancellationReason : null,
            ExternalPatientId = dto.ExternalPatientId,
            SourceSystem = DtoMapper.CleanSourceSystem(dto.SourceSystem),
            CareUnitId = dto.CareUnitId ?? dto.ReferralDepartment,
            DepartmentId = dto.DepartmentId ?? dto.CareUnitId ?? dto.ReferralDepartment,
            TreatmentUnitId = treatmentUnitId,
            RegionCode = dto.RegionCode,
            IntegrationStatus = DtoMapper.ParseIntegrationStatus(dto.IntegrationStatus)
        };

        if (dto.ClinicalDetails is not null)
            booking.ClinicalDetails = DtoMapper.ToEntity(dto.ClinicalDetails);

        _context.Bookings.Add(booking);
        if (!string.IsNullOrWhiteSpace(booking.DepartmentId)) await PatientUnitMemberships.Grant(_context,booking.PatientId,booking.DepartmentId,"department","Remiss från avdelningen");
        if (!string.IsNullOrWhiteSpace(booking.TreatmentUnitId))
            await PatientUnitMemberships.Grant(_context,booking.PatientId,booking.TreatmentUnitId,"treatment","Remiss till behandlingsenheten");
        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (BookingSlots.IsUniqueConstraintViolation(ex))
        {
            // The unique filtered index is the atomic final guard when two requests race past the pre-check.
            return OperationResult<BookingDto>.Fail(BookingErrors.SlotOccupied);
        }

        // Return the persisted values, including the database-generated booking identifier.
        var result = DtoMapper.ToDto(booking);
        return OperationResult<BookingDto>.Ok(result);
    }
}
