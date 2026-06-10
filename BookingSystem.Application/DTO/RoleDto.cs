// Backend purpose: Defines API data contracts related to RoleDto.
namespace BookingSystem.Application.DTO
{
    public class RoleDto
    {
        public int RoleId { get; set; }
        public string? RoleName { get; set; } = string.Empty;
    }

    public class CreateRoleDto
    {
        public string? RoleName { get; set; } = string.Empty;
    }
}
