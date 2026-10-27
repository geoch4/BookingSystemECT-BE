// Backend purpose: One definition of when a treatment slot is taken, shared by every flow that books.
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BookingSystem.Domain.Enums;
using BookingSystem.Infrastructure;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace BookingSystem.Application.Common;

public static class BookingSlots
{
    // A slot is a minute at a treatment unit. Only confirmed bookings occupy it; a pending
    // referral's Date is a placeholder for the requested day and never blocks anyone.
    // Matches the filtered unique index IX_Bookings_TreatmentUnitId_Date.
    public static Task<bool> IsOccupiedAsync(
        AppDbContext context,
        string? treatmentUnitId,
        DateTime slot,
        int? excludeBookingId,
        CancellationToken cancellationToken = default)
    {
        var minute = NormalizeToMinute(slot);
        var unit = string.IsNullOrWhiteSpace(treatmentUnitId) ? null : treatmentUnitId;

        return context.Bookings.AnyAsync(
            b => b.Status == BookingStatus.Confirmed &&
                 b.Date == minute &&
                 b.TreatmentUnitId == unit &&
                 (excludeBookingId == null || b.BookingId != excludeBookingId),
            cancellationToken);
    }

    // The frontend treats a minute as the slot key, so seconds cannot be used to bypass the lock.
    public static DateTime NormalizeToMinute(DateTime date)
    {
        return new DateTime(date.Year, date.Month, date.Day, date.Hour, date.Minute, 0, date.Kind);
    }

    // SQL Server uses 2601 and 2627 for duplicate unique-index or unique-constraint values.
    public static bool IsUniqueConstraintViolation(DbUpdateException exception)
    {
        return exception.InnerException is SqlException { Number: 2601 or 2627 };
    }
}
