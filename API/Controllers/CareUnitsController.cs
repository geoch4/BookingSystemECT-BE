using BookingSystem.Application.Authorization;
using BookingSystem.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
namespace API.Controllers;
[ApiController]
[Authorize(Policy = AccessPolicies.CareStaff)]
[Route("api/care-units")]
public class CareUnitsController(AppDbContext context) : ControllerBase
{
    [HttpGet("departments")]
    public async Task<IActionResult> GetDepartments()
    {
        var users = await context.Users.Include(u=>u.Role).ToListAsync();
        return Ok(users.Where(u=>AccessRoles.IsDepartmentUser(u.Role?.RoleName) && !string.IsNullOrWhiteSpace(u.CareUnitId)).Select(u=>u.CareUnitId!).Distinct().OrderBy(id=>id));
    }
    [HttpGet("treatment")]
    public async Task<IActionResult> GetTreatmentUnits() => Ok(await TreatmentUnitDirectory.GetIds(context));
}
