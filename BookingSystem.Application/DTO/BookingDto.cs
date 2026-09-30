// Backend purpose: Defines API data contracts related to BookingDto.
using System;
using BookingSystem.Domain.Enums;

namespace BookingSystem.Application.DTO
{
    public class BookingDto
    {
        public int BookingId { get; set; }
        public DateTime Date { get; set; }
        public int PatientId { get; set; }
        public TreatmentType TreatmentType { get; set; }
        public int CreatedById { get; set; }
        public DateTime CreatedAt { get; set; }
        public BookingPriority Priority { get; set; }
        public BookingStatus Status { get; set; }
        public AssessmentStatus HealthDeclarationStatus { get; set; }
        public AssessmentStatus AnesthesiaAssessmentStatus { get; set; }
        public string? ReferralDepartment { get; set; }
        public string? ReferralContactName { get; set; }
        public string? ReferralContactPhone { get; set; }
        public string? CancellationReason { get; set; }
        public string? ExternalPatientId { get; set; }
        public string SourceSystem { get; set; } = "Manual";
        public string? CareUnitId { get; set; }
        public string? RegionCode { get; set; }
        public string IntegrationStatus { get; set; } = "Manual";
    }

    public class CreateBookingDto
    {
        public DateTime Date { get; set; }
        public int PatientId { get; set; }
        public TreatmentType TreatmentType { get; set; }
        public int CreatedById { get; set; }
        public DateTime CreatedAt { get; set; }
        public BookingPriority Priority { get; set; }
        public BookingStatus Status { get; set; }
        public AssessmentStatus HealthDeclarationStatus { get; set; }
        public AssessmentStatus AnesthesiaAssessmentStatus { get; set; }
        public string? ReferralDepartment { get; set; }
        public string? ReferralContactName { get; set; }
        public string? ReferralContactPhone { get; set; }
        public string? CancellationReason { get; set; }
        public string? ExternalPatientId { get; set; }
        public string? SourceSystem { get; set; }
        public string? CareUnitId { get; set; }
        public string? RegionCode { get; set; }
        public string? IntegrationStatus { get; set; }
    }
}
