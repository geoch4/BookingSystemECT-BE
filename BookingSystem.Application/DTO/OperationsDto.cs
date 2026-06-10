// Backend purpose: Defines API data contracts related to OperationsDto.
using System;
using BookingSystem.Domain.Enums;

namespace BookingSystem.Application.DTO;

// Represents staffing capacity and requirements returned for a specific date.
public class StaffingStatusDto
{
    public DateTime Date { get; set; }
    public int AnesthesiaAvailable { get; set; }
    public int AnesthesiaRequired { get; set; }
    public int NursesAvailable { get; set; }
    public int NursesRequired { get; set; }
    public int DoctorsAvailable { get; set; }
    public int DoctorsRequired { get; set; }
    public TimeSpan? BreakStart { get; set; }
    public TimeSpan? BreakEnd { get; set; }
    public DateTime UpdatedAt { get; set; }
}

// Represents an active standby queue entry enriched with the patient's display name.
public class StandbyEntryDto
{
    public int StandbyEntryId { get; set; }
    public int PatientId { get; set; }
    public string PatientName { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public StandbyUrgency Urgency { get; set; }
    public DateTime CreatedAt { get; set; }
}

// Contains the client-editable fields required to place a patient on standby.
public class CreateStandbyEntryDto
{
    public int PatientId { get; set; }
    public string Department { get; set; } = string.Empty;
    public StandbyUrgency Urgency { get; set; }
}

// Aggregates booking and assessment metrics for the requested reporting period.
public class BookingStatisticsDto
{
    public DateTime From { get; set; }
    public DateTime To { get; set; }
    public int Total { get; set; }
    public int Confirmed { get; set; }
    public int Pending { get; set; }
    public int Cancelled { get; set; }
    public int Urgent { get; set; }
    public int Ect { get; set; }
    public int Rtms { get; set; }
    public double AverageLeadTimeDays { get; set; }
    public int MissingHealthDeclaration { get; set; }
    public int MissingAnesthesiaAssessment { get; set; }
}
