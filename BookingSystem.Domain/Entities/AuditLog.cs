using System.ComponentModel.DataAnnotations;

namespace BookingSystem.Domain.Entities;

public class AuditLog
{
    [Key]
    public long AuditLogId { get; set; }

    [MaxLength(80)]
    public string EntityName { get; set; } = string.Empty;

    [MaxLength(80)]
    public string EntityId { get; set; } = string.Empty;

    [MaxLength(40)]
    public string Action { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public int? UserId { get; set; }

    [MaxLength(160)]
    public string? UserName { get; set; }

    [MaxLength(80)]
    public string? Role { get; set; }

    [MaxLength(120)]
    public string? CareUnitId { get; set; }

    [MaxLength(80)]
    public string? IpAddress { get; set; }

    [MaxLength(40)]
    public string? RequestMethod { get; set; }

    [MaxLength(300)]
    public string? RequestPath { get; set; }

    [MaxLength(500)]
    public string? Reason { get; set; }

    public string? ChangedFields { get; set; }
}
