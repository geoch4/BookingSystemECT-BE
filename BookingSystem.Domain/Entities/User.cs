// Backend purpose: Defines the persisted domain entity User.
using System.ComponentModel.DataAnnotations;

namespace BookingSystem.Domain.Entities
{
    public class User
    {
        [Key]
        public int UserId { get; set; }
       
        public string Fullname { get; set; } 
       
        public string Email { get; set; } 
      
        public string PasswordHash { get; set; } 
        public bool IsDeleted { get; set; } = false;

        [MaxLength(120)]
        public string? CareUnitId { get; set; }

        // Navigation properties and foreign keys.
        public Role Role  { get; set; }
        public int RoleId { get; set; }
    }
}
