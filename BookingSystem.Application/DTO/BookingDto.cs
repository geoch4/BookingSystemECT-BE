// Backend purpose: Defines API data contracts related to BookingDto.
using System;
using System.Collections.Generic;
using BookingSystem.Domain.Enums;

namespace BookingSystem.Application.DTO
{
    public class BookingDto
    {
        public int BookingId { get; set; }
        public DateTime Date { get; set; }
        public DateTime? RequestedStartDate { get; set; }
        public int PatientId { get; set; }
        public TreatmentType TreatmentType { get; set; }
        public int CreatedById { get; set; }
        public DateTime CreatedAt { get; set; }
        public BookingPriority Priority { get; set; }
        public BookingStatus Status { get; set; }
        public int? DepartmentPriority { get; set; }
        public AssessmentStatus HealthDeclarationStatus { get; set; }
        public AssessmentStatus AnesthesiaAssessmentStatus { get; set; }
        public string? ReferralDepartment { get; set; }
        public string? ReferralContactName { get; set; }
        public string? ReferralContactPhone { get; set; }
        public string? CancellationReason { get; set; }
        public string? ExternalPatientId { get; set; }
        public string SourceSystem { get; set; } = "Manual";
        public string? CareUnitId { get; set; }
        public string? DepartmentId { get; set; }
        public string? TreatmentUnitId { get; set; }

        public string? RegionCode { get; set; }
        public string IntegrationStatus { get; set; } = "Manual";
    }

    public class CreateBookingDto
    {
        public DateTime Date { get; set; }
        // The day the referring department asks for. Defaults to Date's day when omitted.
        public DateTime? RequestedStartDate { get; set; }
        // Clinical basis for the referral; stored once when the referral is created.
        public ReferralClinicalDetailsDto? ClinicalDetails { get; set; }
        public int PatientId { get; set; }
        public TreatmentType TreatmentType { get; set; }
        public int CreatedById { get; set; }
        public DateTime CreatedAt { get; set; }
        public BookingPriority Priority { get; set; }
        public BookingStatus Status { get; set; }
        public int? DepartmentPriority { get; set; }
        public AssessmentStatus HealthDeclarationStatus { get; set; }
        public AssessmentStatus AnesthesiaAssessmentStatus { get; set; }
        public string? ReferralDepartment { get; set; }
        public string? ReferralContactName { get; set; }
        public string? ReferralContactPhone { get; set; }
        public string? CancellationReason { get; set; }
        public string? ExternalPatientId { get; set; }
        public string? SourceSystem { get; set; }
        public string? CareUnitId { get; set; }
        public string? DepartmentId { get; set; }
        public string? TreatmentUnitId { get; set; }

        public string? RegionCode { get; set; }
        public string? IntegrationStatus { get; set; }
    }

    public class ReferralClinicalDetailsDto
    {
        public string? Indication { get; set; }
        public string? AnesthesiaInfo { get; set; }
        public string? SomaticRisk { get; set; }
        public bool? FastingConfirmed { get; set; }
        public string? BloodPressure { get; set; }
        public string? WeightKg { get; set; }
        public string? Allergies { get; set; }
        public string? Medications { get; set; }
        public string? AdditionalInfo { get; set; }
        public string? RtmsSessionPlan { get; set; }
        public string? PreviousTreatments { get; set; }
        public string? TreatmentGoal { get; set; }
        public bool? ContraindicationChecklistComplete { get; set; }
        public bool? MetalImplantsOrDevices { get; set; }
        public bool? SeizureHistory { get; set; }
        public List<string> CompletedDocuments { get; set; } = new();
    }
}
