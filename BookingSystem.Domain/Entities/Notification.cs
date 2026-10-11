// Backend purpose: Defines the persisted domain entity Notification.
using System.ComponentModel.DataAnnotations;

namespace BookingSystem.Domain.Entities
{
    public class Notification
    {
        [Key]
        public int NotificationId { get; set; }

        [Required]
        public string Message { get; set; } = string.Empty;

        public DateTime SentAt { get; set; }

        public int RecipientId { get; set; }
        public User? Recipient { get; set; }
        public bool IsRead { get; set; }
    }
}
