// Backend purpose: Provides shared application behavior for BookingErrors.
namespace BookingSystem.Application.Common;

public static class BookingErrors
{
    public const string SlotOccupied = "The selected booking time is already occupied.";
    public const string NotFound = "Booking not found";
}
