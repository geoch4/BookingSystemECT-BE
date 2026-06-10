// Backend purpose: Defines API data contracts related to LoginDto.


namespace BookingSystem.Application.DTO
{
    public class LoginDto
    {
        public string Email { get; set; } = null!;

        public string Password { get; set; } = null!;
    }



}
