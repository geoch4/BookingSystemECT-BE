// Backend purpose: Defines the persisted domain entity StaffingStatus.
using System.ComponentModel.DataAnnotations;

namespace BookingSystem.Domain.Entities;

public class StaffingStatus
{
    [Key]
    public int StaffingStatusId { get; set; }

    // One row represents the available and required clinical staffing for one date.
    public DateTime Date { get; set; }
    public int AnesthesiaAvailable { get; set; }
    public int AnesthesiaRequired { get; set; }
    public int NursesAvailable { get; set; }
    public int NursesRequired { get; set; }
    public int DoctorsAvailable { get; set; }
    public int DoctorsRequired { get; set; }

    // The optional break interval lets the frontend avoid presenting unavailable capacity.
    public TimeSpan? BreakStart { get; set; }
    public TimeSpan? BreakEnd { get; set; }
    public DateTime UpdatedAt { get; set; }
}
