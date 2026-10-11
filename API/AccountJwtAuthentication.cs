using System.Text;
using BookingSystem.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
namespace API;
public static class AccountJwtAuthentication
{
    public static IServiceCollection AddAccountJwtAuthentication(this IServiceCollection services,IConfiguration configuration)
    {
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options=> {
            options.TokenValidationParameters=new TokenValidationParameters {
                ValidateIssuer=true,ValidateAudience=true,ValidateLifetime=true,ValidateIssuerSigningKey=true,
                ValidIssuer=configuration["Jwt:Issuer"],ValidAudience=configuration["Jwt:Audience"],
                IssuerSigningKey=new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["Jwt:SecretKey"]!)),
                ClockSkew=TimeSpan.Zero
            };
            options.Events=new JwtBearerEvents {OnTokenValidated=async ctx=> {
                var db=ctx.HttpContext.RequestServices.GetRequiredService<AppDbContext>();
                if (!await AccountSessionValidator.IsCurrent(db,ctx.Principal,ctx.HttpContext.RequestAborted)) ctx.Fail("Inloggningen har återkallats. Logga in igen.");
            }};
        });
        return services;
    }
}
