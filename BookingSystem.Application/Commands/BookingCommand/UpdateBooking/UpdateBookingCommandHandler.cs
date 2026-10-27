// Backend purpose: Defines the write-side application flow for UpdateBookingCommandHandler.
using System.Threading;
using System.Threading.Tasks;
using System;
using BookingSystem.Application.Common;
using BookingSystem.Application.Mappings;
using BookingSystem.Domain.Enums;
using BookingSystem.Infrastructure;
using MediatR;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace BookingSystem.Application.Commands.BookingCommand.UpdateBooking;

public class UpdateBookingCommandHandler : IRequestHandler<UpdateBookingCommand,OperationResult<bool>>
{
    private readonly AppDbContext _context;

    public UpdateBookingCommandHandler(AppDbContext context)
    {
        _context = context;
    }

    public async Task<OperationResult<bool>> Handle(UpdateBookingCommand request, CancellationToken cancellationToken)
    {
        var booking = await _context.Bookings
            .FirstOrDefaultAsync(b => b.BookingId == request.Id, cancellationToken);

        if (booking == null)
            return OperationResult<bool>.Fail(BookingErrors.NotFound);

        var dto = request.Dto;
        var bookingDate = BookingSlots.NormalizeToMinute(dto.Date);

        // Exclude the booking being edited; only confirmed bookings at the same unit compete for the slot.
        if (dto.Status == BookingStatus.Confirmed &&
            await BookingSlots.IsOccupiedAsync(_context, booking.TreatmentUnitId, bookingDate, request.Id, cancellationToken))
        {
            return OperationResult<bool>.Fail(BookingErrors.SlotOccupied);
        }

        booking.Date = bookingDate;
        if (dto.RequestedStartDate is not null) booking.RequestedStartDate = dto.RequestedStartDate.Value.Date;
        booking.PatientId = dto.PatientId;
        booking.TreatmentType = dto.TreatmentType;
        booking.CreatedByUserId = dto.CreatedById;
        booking.CreatedAt = dto.CreatedAt;
        booking.Priority = dto.Priority;
        booking.Status = dto.Status;
        booking.DepartmentPriority = dto.DepartmentPriority;
        booking.HealthDeclarationStatus = dto.HealthDeclarationStatus;
        booking.AnesthesiaAssessmentStatus = dto.AnesthesiaAssessmentStatus;
        booking.ReferralDepartment = dto.ReferralDepartment;
        booking.ReferralContactName = dto.ReferralContactName;
        booking.ReferralContactPhone = dto.ReferralContactPhone;
        booking.CancellationReason = dto.Status == BookingStatus.Cancelled ? dto.CancellationReason : null;
        booking.ExternalPatientId = dto.ExternalPatientId;
        booking.SourceSystem = DtoMapper.CleanSourceSystem(dto.SourceSystem);
        booking.CareUnitId = dto.CareUnitId ?? dto.ReferralDepartment;
        booking.DepartmentId = dto.DepartmentId ?? booking.DepartmentId ?? booking.CareUnitId;
        booking.RegionCode = dto.RegionCode;
        booking.IntegrationStatus = DtoMapper.ParseIntegrationStatus(dto.IntegrationStatus);

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (BookingSlots.IsUniqueConstraintViolation(ex))
        {
            // The database index resolves concurrent updates atomically even if both pre-checks succeeded.
            return OperationResult<bool>.Fail(BookingErrors.SlotOccupied);
        }

        return OperationResult<bool>.Ok(true);
    }
}
