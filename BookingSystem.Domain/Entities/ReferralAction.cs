using System.ComponentModel.DataAnnotations;
using BookingSystem.Domain.Enums;

namespace BookingSystem.Domain.Entities;

public class ReferralAction
{
    [Key]
    public int ReferralActionId { get; set; }

    public int ReferralId { get; set; }

    public ReferralActionType ActionType { get; set; }

    [MaxLength(160)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string Comment { get; set; } = string.Empty;

    public BookingPriority? Priority { get; set; }

    public DateTime? ConfirmedDateTime { get; set; }

    public DateTime CreatedAt { get; set; }

    public int? CreatedById { get; set; }
}
