// Backend purpose: Defines API data contracts related to NotificationDto.
using System;

namespace BookingSystem.Application.DTO
{
    public class NotificationDto
    {
        public int NotificationId { get; set; }
        public string Message { get; set; } = string.Empty;
        public DateTime SentAt { get; set; }
        public int RecipientId { get; set; }
        public bool IsRead { get; set; }
    }
   
        public class CreateNotificationDto
        {
            public string Message { get; set; } = string.Empty;
            public int RecipientId { get; set; }
            public bool IsRead { get; set; }
        }
    

}
