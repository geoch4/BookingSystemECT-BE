// Backend purpose: Defines the persisted domain entity User.
using System.ComponentModel.DataAnnotations;

namespace BookingSystem.Domain.Entities
{
    public class User
    {
        [Key]
        public int UserId { get; set; }
       
        public string Fullname { get; set; } = string.Empty;
       
        public string Email { get; set; } = string.Empty;
      
        public string PasswordHash { get; set; } = string.Empty;
        [MaxLength(32)]
        public string SessionVersion { get; set; } = Guid.NewGuid().ToString("N");
        public bool IsDeleted { get; set; } = false;

        [MaxLength(120)]
        public string? CareUnitId { get; set; }

        // Navigation properties and foreign keys.
        public Role Role  { get; set; } = null!;
        public int RoleId { get; set; }
    }
}
