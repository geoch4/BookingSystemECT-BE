// Backend purpose: Defines object mappings shared by application flows.
namespace BookingSystem.Application.Mappings;

// This placeholder documents planned mappings while handlers currently map DTOs explicitly.
public class MappingProfile
{
    public MappingProfile()
    {
        // TODO: Add Booking-to-BookingDto mapping if AutoMapper is reintroduced.
        // CreateMap<Booking, BookingDto>().ReverseMap();

        // TODO: Add CreateBookingDto-to-Booking mapping if creation moves to AutoMapper.
        // CreateMap<CreateBookingDto, Booking>();

        // TODO: Add mappings for other entities only when the application starts using this profile.
        // Example:
        // CreateMap<User, UserDto>().ReverseMap();
        // CreateMap<CreateUserDto, User>();
    }
  
}
