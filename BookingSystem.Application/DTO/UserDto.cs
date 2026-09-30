// Backend purpose: Defines API data contracts related to UserDto.

namespace BookingSystem.Application.DTO
{
    public class UserDto
    {
        public int UserId { get; set; } 
        public string Email { get; set; } = string.Empty;
        public string Fullname { get; set; } = string.Empty;
        public int RoleId { get; set; }
        public string? RoleName { get; set; } = string.Empty;
        public string? CareUnitId { get; set; }
    }

    public class CreateUserDto
    {
        public string Fullname { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public int RoleId { get; set; }
        public string? CareUnitId { get; set; }
    }

    public class UpdateUserDto
    {
        public string? Fullname { get; set; } 
        public string? Email { get; set; } 
        public string? Password { get; set; } 
        public int? RoleId { get; set; }
        public string? CareUnitId { get; set; }
    }
}
