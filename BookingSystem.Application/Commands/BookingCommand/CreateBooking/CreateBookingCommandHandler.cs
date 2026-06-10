// Backend purpose: Defines the write-side application flow for CreateBookingCommandHandler.
using System.Threading;
using System.Threading.Tasks;
using System;
using BookingSystem.Application.Common;
using BookingSystem.Application.DTO;
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
        // The frontend treats a minute as the slot key, so seconds cannot be used to bypass the lock.
        var bookingDate = NormalizeToMinute(dto.Date);

        // Return a friendly conflict before writing when the slot is already known to be occupied.
        if (dto.Status != BookingStatus.Cancelled &&
            await _context.Bookings.AnyAsync(
                b => b.Date == bookingDate && b.Status != BookingStatus.Cancelled,
                cancellationToken))
        {
            return OperationResult<BookingDto>.Fail(BookingErrors.SlotOccupied);
        }

        var booking = new Booking
        {
            Date = bookingDate,
            PatientId = dto.PatientId,
            TreatmentType = dto.TreatmentType,
            CreatedByUserId = dto.CreatedById,
            CreatedAt = dto.CreatedAt,
            Priority = dto.Priority,
            Status = dto.Status,
            HealthDeclarationStatus = dto.HealthDeclarationStatus,
            AnesthesiaAssessmentStatus = dto.AnesthesiaAssessmentStatus,
            ReferralDepartment = dto.ReferralDepartment,
            ReferralContactName = dto.ReferralContactName,
            ReferralContactPhone = dto.ReferralContactPhone
        };

        _context.Bookings.Add(booking);
        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
        {
            // The unique filtered index is the atomic final guard when two requests race past the pre-check.
            return OperationResult<BookingDto>.Fail(BookingErrors.SlotOccupied);
        }

        // Return the persisted values, including the database-generated booking identifier.
        var result = new BookingDto
        {
            BookingId = booking.BookingId,
            Date = booking.Date,
            PatientId = booking.PatientId,
            TreatmentType = booking.TreatmentType,
            CreatedById = booking.CreatedByUserId,
            CreatedAt = booking.CreatedAt,
            Priority = booking.Priority,
            Status = booking.Status,
            HealthDeclarationStatus = booking.HealthDeclarationStatus,
            AnesthesiaAssessmentStatus = booking.AnesthesiaAssessmentStatus,
            ReferralDepartment = booking.ReferralDepartment,
            ReferralContactName = booking.ReferralContactName,
            ReferralContactPhone = booking.ReferralContactPhone
        };
        return OperationResult<BookingDto>.Ok(result);
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
