using System;
using BookingSystem.Domain.Enums;

namespace BookingSystem.Application.DTO;

public class ReferralDto
{
    public int ReferralId { get; set; }
    public int? BookingId { get; set; }
    public int PatientId { get; set; }
    public TreatmentType TreatmentTypeId { get; set; }
    public string Department { get; set; } = string.Empty;
    public string? ContactName { get; set; }
    public string? ContactPhone { get; set; }
    public BookingPriority Priority { get; set; }
    public ReferralStatus Status { get; set; }
    public DateTime? RequestedStartDate { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? ExternalPatientId { get; set; }
    public string SourceSystem { get; set; } = "Manual";
    public string? CareUnitId { get; set; }
    public string? RegionCode { get; set; }
    public string IntegrationStatus { get; set; } = "Manual";
}

public class ReferralActionDto
{
    public int ReferralActionId { get; set; }
    public int ReferralId { get; set; }
    public ReferralActionType ActionType { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Comment { get; set; } = string.Empty;
    public BookingPriority? Priority { get; set; }
    public DateTime? ConfirmedDateTime { get; set; }
    public DateTime CreatedAt { get; set; }
    public int? CreatedById { get; set; }
}

public class CreateReferralActionDto
{
    public ReferralActionType ActionType { get; set; }
    public string Comment { get; set; } = string.Empty;
    public BookingPriority? Priority { get; set; }
    public DateTime? ConfirmedDateTime { get; set; }
    public int? CreatedById { get; set; }
}

public class ReferralTimelineEventDto
{
    public string Title { get; set; } = string.Empty;
    public string Detail { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string Tone { get; set; } = "info";
}
