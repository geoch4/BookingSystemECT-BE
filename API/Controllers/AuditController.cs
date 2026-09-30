using BookingSystem.Infrastructure;
using BookingSystem.Application.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Controllers;

[ApiController]
[Authorize(Policy = AccessPolicies.OnlyAdmin)]
[Route("api/audit")]
public class AuditController : ControllerBase
{
    private readonly AppDbContext _context;

    public AuditController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetAuditLogs(
        [FromQuery] string? entityName,
        [FromQuery] string? entityId,
        [FromQuery] int limit = 100,
        CancellationToken cancellationToken = default)
    {
        var safeLimit = Math.Clamp(limit, 1, 500);
        var query = _context.AuditLogs.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(entityName))
            query = query.Where(log => log.EntityName == entityName);

        if (!string.IsNullOrWhiteSpace(entityId))
            query = query.Where(log => log.EntityId == entityId);

        var logs = await query
            .OrderByDescending(log => log.CreatedAt)
            .Take(safeLimit)
            .Select(log => new
            {
                log.AuditLogId,
                log.EntityName,
                log.EntityId,
                log.Action,
                log.CreatedAt,
                log.UserId,
                log.UserName,
                log.Role,
                log.CareUnitId,
                log.IpAddress,
                log.RequestMethod,
                log.RequestPath,
                log.Reason,
                log.ChangedFields
            })
            .ToListAsync(cancellationToken);

        return Ok(logs);
    }
}
