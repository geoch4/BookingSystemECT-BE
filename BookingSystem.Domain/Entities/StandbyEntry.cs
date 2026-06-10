// Backend purpose: Defines the persisted domain entity StandbyEntry.
using System.ComponentModel.DataAnnotations;
using BookingSystem.Domain.Enums;

namespace BookingSystem.Domain.Entities;

public class StandbyEntry
{
    [Key]
    public int StandbyEntryId { get; set; }
    public int PatientId { get; set; }
    public Patient Patient { get; set; } = null!;

    // Queue metadata is stored with the patient reference for cross-device consistency.
    [MaxLength(120)]
    public string Department { get; set; } = string.Empty;
    public StandbyUrgency Urgency { get; set; }

    // Inactive entries remain in the database as history instead of being physically deleted.
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
}
