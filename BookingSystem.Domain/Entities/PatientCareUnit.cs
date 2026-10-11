using System.ComponentModel.DataAnnotations;
namespace BookingSystem.Domain.Entities;
public class PatientCareUnit
{
    public int PatientId { get; set; }
    public Patient Patient { get; set; } = null!;
    [MaxLength(120)] public string UnitId { get; set; } = string.Empty;
    [MaxLength(20)] public string Kind { get; set; } = "department";
    public bool IsActive { get; set; } = true;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    [MaxLength(500)] public string Reason { get; set; } = string.Empty;
}
