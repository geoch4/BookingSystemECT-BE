// Backend purpose: Defines API data contracts related to PatientDto.

namespace BookingSystem.Application.DTO
{
    // Returned by patient read endpoints and includes the database identifier.
    public class PatientDto
    {
        public int PatientId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string SocialSecurityNumber { get; set; } = string.Empty;
    }

    // Accepted by patient create and update endpoints without exposing persistence fields.
    public class CreatePatientDto
    {
        public string FullName { get; set; } = string.Empty;
        public string SocialSecurityNumber { get; set; } = string.Empty;
    }
}
