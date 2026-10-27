// Backend purpose: Persists the clinical basis a department submits with an ECT or rTMS referral.
using System.ComponentModel.DataAnnotations;

namespace BookingSystem.Domain.Entities
{
    public class ReferralClinicalDetails
    {
        // One row per referral; the key is the referral's booking id.
        [Key]
        public int BookingId { get; set; }
        public Booking Booking { get; set; } = null!;

        // Shared by ECT and rTMS.
        [MaxLength(2000)]
        public string? Indication { get; set; }

        // ECT
        [MaxLength(2000)]
        public string? AnesthesiaInfo { get; set; }

        [MaxLength(2000)]
        public string? SomaticRisk { get; set; }

        // Health declaration (ECT)
        public bool? FastingConfirmed { get; set; }

        [MaxLength(40)]
        public string? BloodPressure { get; set; }

        [MaxLength(20)]
        public string? WeightKg { get; set; }

        [MaxLength(500)]
        public string? Allergies { get; set; }

        [MaxLength(2000)]
        public string? Medications { get; set; }

        [MaxLength(2000)]
        public string? AdditionalInfo { get; set; }

        // rTMS
        [MaxLength(20)]
        public string? RtmsSessionPlan { get; set; }

        [MaxLength(2000)]
        public string? PreviousTreatments { get; set; }

        [MaxLength(1000)]
        public string? TreatmentGoal { get; set; }

        public bool? ContraindicationChecklistComplete { get; set; }
        public bool? MetalImplantsOrDevices { get; set; }
        public bool? SeizureHistory { get; set; }

        // Comma-separated keys of the supporting documents the department ticked as attached.
        [MaxLength(500)]
        public string? CompletedDocuments { get; set; }
    }
}
