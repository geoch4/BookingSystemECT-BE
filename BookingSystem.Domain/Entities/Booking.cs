// Backend purpose: Defines the persisted domain entity Booking.
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using BookingSystem.Domain.Enums;


namespace BookingSystem.Domain.Entities
{
    public class Booking
    {
        [Key]
        public int BookingId { get; set; }
        [Required]
        public DateTime Date { get; set; }
        [Required]

        public int PatientId { get; set; }
        public Patient Patient { get; set; }

        public TreatmentType TreatmentType { get; set; }

        [Column("CreatedById")]
        public int CreatedByUserId { get; set; }
        public User CreatedBy { get; set; }

        public DateTime CreatedAt { get; set; }

        [Required]
        public BookingPriority Priority { get; set; }
        [Required]
        public BookingStatus Status { get; set; }

        public AssessmentStatus HealthDeclarationStatus { get; set; }
        public AssessmentStatus AnesthesiaAssessmentStatus { get; set; }

        // Referral details are persisted so every client sees the same information.
        [MaxLength(120)]
        public string? ReferralDepartment { get; set; }

        [MaxLength(120)]
        public string? ReferralContactName { get; set; }

        [MaxLength(40)]
        public string? ReferralContactPhone { get; set; }
    }
}
