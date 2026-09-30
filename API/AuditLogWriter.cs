using BookingSystem.Domain;
using BookingSystem.Domain.Entities;
using BookingSystem.Infrastructure;

namespace API;

public class AuditLogWriter
{
    private readonly AppDbContext _context;
    private readonly IAuditContext _auditContext;

    public AuditLogWriter(AppDbContext context, IAuditContext auditContext)
    {
        _context = context;
        _auditContext = auditContext;
    }

    public async Task RecordReadAsync(
        string entityName,
        string entityId,
        CancellationToken cancellationToken = default)
    {
        _context.AuditLogs.Add(new AuditLog
        {
            EntityName = entityName,
            EntityId = entityId,
            Action = "Viewed",
            CreatedAt = DateTime.UtcNow,
            UserId = _auditContext.UserId,
            UserName = _auditContext.UserName ?? "system",
            Role = _auditContext.Role,
            CareUnitId = _auditContext.CareUnitId,
            IpAddress = _auditContext.IpAddress,
            RequestMethod = _auditContext.RequestMethod,
            RequestPath = _auditContext.RequestPath,
            Reason = _auditContext.Reason
        });

        await _context.SaveChangesAsync(cancellationToken);
    }
}
