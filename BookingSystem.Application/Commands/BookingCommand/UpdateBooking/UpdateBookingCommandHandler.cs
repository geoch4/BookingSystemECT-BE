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
        // Keep updates on the same minute-based slot definition used by booking creation.
        var bookingDate = NormalizeToMinute(dto.Date);

        // Exclude the booking being edited while checking whether another active booking owns the target slot.
        if (dto.Status != BookingStatus.Cancelled &&
            await _context.Bookings.AnyAsync(
                b => b.BookingId != request.Id &&
                     b.Date == bookingDate &&
                     b.Status != BookingStatus.Cancelled,
                cancellationToken))
        {
            return OperationResult<bool>.Fail(BookingErrors.SlotOccupied);
        }

        booking.Date = bookingDate;
        booking.PatientId = dto.PatientId;
        booking.TreatmentType = dto.TreatmentType;
        booking.CreatedByUserId = dto.CreatedById;
        booking.CreatedAt = dto.CreatedAt;
        booking.Priority = dto.Priority;
        booking.Status = dto.Status;
        booking.HealthDeclarationStatus = dto.HealthDeclarationStatus;
        booking.AnesthesiaAssessmentStatus = dto.AnesthesiaAssessmentStatus;
        booking.ReferralDepartment = dto.ReferralDepartment;
        booking.ReferralContactName = dto.ReferralContactName;
        booking.ReferralContactPhone = dto.ReferralContactPhone;
        booking.CancellationReason = dto.Status == BookingStatus.Cancelled ? dto.CancellationReason : null;
        booking.ExternalPatientId = dto.ExternalPatientId;
        booking.SourceSystem = DtoMapper.CleanSourceSystem(dto.SourceSystem);
        booking.CareUnitId = dto.CareUnitId ?? dto.ReferralDepartment;
        booking.RegionCode = dto.RegionCode;
        booking.IntegrationStatus = DtoMapper.ParseIntegrationStatus(dto.IntegrationStatus);

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
        {
            // The database index resolves concurrent updates atomically even if both pre-checks succeeded.
            return OperationResult<bool>.Fail(BookingErrors.SlotOccupied);
        }

        return OperationResult<bool>.Ok(true);
    }

    private static bool IsUniqueConstraintViolation(DbUpdateException exception)
    {
        // SQL Server uses 2601 and 2627 for duplicate unique-index or unique-constraint values.
        return exception.InnerException is SqlException { Number: 2601 or 2627 };
    }

    private static DateTime NormalizeToMinute(DateTime date)
    {
        return new DateTime(date.Year, date.Month, date.Day, date.Hour, date.Minute, 0, date.Kind);
    }
}
