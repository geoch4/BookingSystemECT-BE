using BookingSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
namespace BookingSystem.Infrastructure;
public static class PatientUnitMemberships
{
    public static async Task Grant(AppDbContext context, int patientId, string unitId, string kind, string reason)
    {
        var membership = await context.PatientCareUnits.FindAsync(patientId, unitId, kind);
        if (membership is null) { membership = new PatientCareUnit {PatientId=patientId,UnitId=unitId,Kind=kind}; context.PatientCareUnits.Add(membership); }
        membership.IsActive=true; membership.UpdatedAt=DateTime.UtcNow; membership.Reason=reason;
    }
    public static async Task RevokeUnusedTreatment(AppDbContext context, int patientId, string? oldUnit)
    {
        if (string.IsNullOrWhiteSpace(oldUnit)) return;
        // Evaluate after tracking: an assignment changed in this transaction must not count as its old unit.
        var bookings = await context.Bookings.Where(b=>b.PatientId==patientId).ToListAsync();
        if (bookings.Any(b=>b.TreatmentUnitId==oldUnit && b.Status!=BookingSystem.Domain.Enums.BookingStatus.Cancelled)) return;
        var membership = await context.PatientCareUnits.FindAsync(patientId,oldUnit,"treatment");
        if (membership is not null) { membership.IsActive=false; membership.UpdatedAt=DateTime.UtcNow; membership.Reason="Behandlingsenhet ändrad"; }
    }
}
