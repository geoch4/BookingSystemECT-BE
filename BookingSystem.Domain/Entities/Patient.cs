// Backend purpose: Defines the persisted domain entity Patient.
using System.ComponentModel.DataAnnotations;
using BookingSystem.Domain.Enums;

namespace BookingSystem.Domain.Entities
{
    public class Patient
    {
        [Key]
        public int PatientId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string SocialSecurityNumber { get; set; } = string.Empty;

        [MaxLength(160)]
        public string? ExternalPatientId { get; set; }

        [MaxLength(80)]
        public string SourceSystem { get; set; } = "Manual";

        [MaxLength(20)]
        public string? RegionCode { get; set; }

        public IntegrationStatus IntegrationStatus { get; set; } = IntegrationStatus.Manual;

        public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
    }
}
