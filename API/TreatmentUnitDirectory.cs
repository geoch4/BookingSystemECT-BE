using BookingSystem.Application.Authorization;
using BookingSystem.Infrastructure;
using Microsoft.EntityFrameworkCore;
namespace API;
public static class TreatmentUnitDirectory
{
    public static async Task<string[]> GetIds(AppDbContext context)
    {
        var users = await context.Users.Include(u => u.Role).ToListAsync();
        return users.Where(u => !AccessRoles.IsAdmin(u.Role?.RoleName) && AccessRoles.IsReferralDecisionUser(u.Role?.RoleName) && !string.IsNullOrWhiteSpace(u.CareUnitId))
            .Select(u => u.CareUnitId!).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(id => id).ToArray();
    }
}
