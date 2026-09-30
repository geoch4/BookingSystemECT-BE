using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using BookingSystem.Domain;

namespace API;

public class HttpAuditContext : IAuditContext
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public HttpAuditContext(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public int? UserId
    {
        get
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
                         ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub)
                         ?? User.FindFirstValue("sub");

            return int.TryParse(userId, out var parsedUserId) ? parsedUserId : null;
        }
    }

    public string? UserName => User.FindFirstValue(ClaimTypes.Email) ?? User.Identity?.Name;
    public string? Role => User.FindFirstValue(ClaimTypes.Role);
    public string? CareUnitId => User.FindFirstValue("care_unit");
    public string? IpAddress => HttpContext.Connection.RemoteIpAddress?.ToString();
    public string? RequestMethod => HttpContext.Request.Method;
    public string? RequestPath => HttpContext.Request.Path.Value;
    public string? Reason => HttpContext.Request.Headers.TryGetValue("X-Audit-Reason", out var reason)
        ? reason.ToString()
        : null;

    private HttpContext HttpContext => _httpContextAccessor.HttpContext ?? new DefaultHttpContext();
    private ClaimsPrincipal User => HttpContext.User;
}
