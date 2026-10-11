using BookingSystem.Infrastructure;
using BookingSystem.Domain.Entities;
using BookingSystem.Application.Authorization;
namespace API;
public static class CareUnitPatientAccess
{
    public static IQueryable<Patient> Query(AppDbContext context, string careUnitId, string? role = AccessRoles.Department)
    {
        if (string.IsNullOrWhiteSpace(careUnitId)) return context.Patients.Where(p=>false);
        var kind = AccessRoles.IsDepartmentUser(role) ? "department" : AccessRoles.IsReferralDecisionUser(role) ? "treatment" : null;
        if (kind is null) return context.Patients.Where(p=>false);
        return context.Patients.Where(patient => context.PatientCareUnits.Any(m => m.PatientId==patient.PatientId && m.UnitId==careUnitId && m.Kind==kind && m.IsActive));
    }
    public static IQueryable<Booking> RestrictBookings(AppDbContext context, IQueryable<Booking> query, string? role, string? unit)
    {
        if (AccessRoles.IsAdmin(role)) return query;
        var patients = Query(context,unit ?? "",role);
        return query.Where(b=>patients.Any(p=>p.PatientId==b.PatientId));
    }
}
