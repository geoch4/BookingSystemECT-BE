using System.Security.Claims;
using BookingSystem.Application.Authorization;
using BookingSystem.Infrastructure;
using Microsoft.EntityFrameworkCore;
namespace API;
public static class AccountSessionValidator
{
    public static async Task<bool> IsCurrent(AppDbContext context, ClaimsPrincipal? principal, CancellationToken cancellationToken=default)
    {
        if (principal?.Identity?.IsAuthenticated!=true || !int.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier),out var id)) return false;
        var user=await context.Users.AsNoTracking().Include(u=>u.Role).SingleOrDefaultAsync(u=>u.UserId==id,cancellationToken);
        if (user is null || user.IsDeleted || string.IsNullOrWhiteSpace(user.SessionVersion)) return false;
        var version=principal.FindFirstValue("session_version");
        return !string.IsNullOrWhiteSpace(version) && version==user.SessionVersion
            && principal.FindFirstValue(ClaimTypes.Role)==user.Role?.RoleName
            && (principal.FindFirstValue("care_unit") ?? "")== (user.CareUnitId ?? "")
            && AccessRoles.IsAny(user.Role?.RoleName,AccessRoles.AuthenticatedCareUsers);
    }
}
