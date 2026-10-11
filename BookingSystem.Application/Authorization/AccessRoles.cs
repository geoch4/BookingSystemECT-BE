using System;
using System.Linq;

namespace BookingSystem.Application.Authorization;

public static class AccessRoles
{
    public const string Admin = "Admin";
    public const string Department = "Avdelning";

    public static readonly string[] MedicalResponsible =
    [
        "Medicinskt ansvarig",
        "Överläkare",
        "Underläkare",
        "Läkare"
    ];

    public static readonly string[] TreatmentUnit =
    [
        Admin,
        "ECTSjuksköterska",
        "ECT-koordinator",
        "Behandlingsenhet",
        "Treatment unit"
    ];

    public static readonly string[] ReferralDecisionUsers =
    [
        .. TreatmentUnit,
        .. MedicalResponsible
    ];

    public static readonly string[] DepartmentUsers =
    [
        Department,
        "Remitterande avdelning",
        "Vårdavdelning",
        "Mottagning"
    ];

    public static readonly string[] AuthenticatedCareUsers =
    [
        .. TreatmentUnit,
        .. MedicalResponsible,
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

    public static bool IsReferralDecisionUser(string? role)
    {
        return IsAny(role, ReferralDecisionUsers);
    }

    public static bool IsMedicalResponsible(string? role)
    {
        return IsAny(role, MedicalResponsible);
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
