using System.Threading.RateLimiting;
namespace API;
public static class LoginRateLimiting
{
    public const string Policy="login";
    public static IServiceCollection AddLoginRateLimiting(this IServiceCollection services)
    {
        services.AddRateLimiter(options=> {
            options.RejectionStatusCode=StatusCodes.Status429TooManyRequests;
            options.AddPolicy(Policy,context=>RateLimitPartition.GetFixedWindowLimiter(
                context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                _=>new FixedWindowRateLimiterOptions {PermitLimit=10,Window=TimeSpan.FromMinutes(1),QueueLimit=0,AutoReplenishment=true}));
        });
        return services;
    }
}
