using System;
using System.Linq;

namespace BookingSystem.Application.Authorization;

public static class AccessRoles
{
    public const string Admin = "Admin";
    public const string Department = "Avdelning";

    public static readonly string[] TreatmentUnit =
    [
        Admin,
        "ECTSjuksköterska",
        "ECT-koordinator",
        "Behandlingsenhet",
        "Treatment unit",
        "Överläkare"
    ];

    public static readonly string[] DepartmentUsers =
    [
        Department,
        "Remitterande avdelning",
        "Vårdavdelning"
    ];

    public static readonly string[] AuthenticatedCareUsers =
    [
        .. TreatmentUnit,
        .. DepartmentUsers
    ];

    public static bool IsAdmin(string? role)
    {
        return IsAny(role, Admin);
    }

    public static bool IsTreatmentUnit(string? role)
    {
        return IsAny(role, TreatmentUnit);
    }

    public static bool IsDepartmentUser(string? role)
    {
        return IsAny(role, DepartmentUsers);
    }

    public static bool IsAny(string? role, params string[] allowedRoles)
    {
        return allowedRoles.Any(allowed => string.Equals(role, allowed, StringComparison.OrdinalIgnoreCase));
    }
}
