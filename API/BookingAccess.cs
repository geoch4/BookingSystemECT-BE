using BookingSystem.Application.Authorization;
using BookingSystem.Domain.Entities;
namespace API;
public static class BookingAccess
{
    public static bool CanRead(string? role, string? unit, string? departmentId, string? treatmentUnitId)
    {
        if (AccessRoles.IsAdmin(role)) return true;
        if (string.IsNullOrWhiteSpace(unit)) return false;
        var assigned = AccessRoles.IsDepartmentUser(role) ? departmentId :
            AccessRoles.IsReferralDecisionUser(role) ? treatmentUnitId : null;
        return !string.IsNullOrWhiteSpace(assigned) && string.Equals(unit, assigned, StringComparison.OrdinalIgnoreCase);
    }
    public static IQueryable<Booking> Query(IQueryable<Booking> query, string? role, string? unit)
    {
        if (AccessRoles.IsAdmin(role)) return query;
        if (string.IsNullOrWhiteSpace(unit)) return query.Where(b => false);
        if (AccessRoles.IsDepartmentUser(role)) return query.Where(b => (b.DepartmentId ?? b.CareUnitId ?? b.ReferralDepartment) == unit);
        if (AccessRoles.IsReferralDecisionUser(role)) return query.Where(b => b.TreatmentUnitId == unit);
        return query.Where(b => false);
    }
}
