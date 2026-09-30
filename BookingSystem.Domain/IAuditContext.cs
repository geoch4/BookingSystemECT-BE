namespace BookingSystem.Domain;

public interface IAuditContext
{
    int? UserId { get; }
    string? UserName { get; }
    string? Role { get; }
    string? CareUnitId { get; }
    string? IpAddress { get; }
    string? RequestMethod { get; }
    string? RequestPath { get; }
    string? Reason { get; }
}
