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
    }
}
