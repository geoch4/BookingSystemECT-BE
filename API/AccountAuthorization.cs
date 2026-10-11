using BookingSystem.Application.Authorization;
using Microsoft.AspNetCore.Authorization;
namespace API;
public static class AccountAuthorization
{
    public static IServiceCollection AddAccountAuthorization(this IServiceCollection services)
    {
        services.AddAuthorization(options=> {
            options.AddPolicy(AccessPolicies.OnlyAdmin,p=>p.Requirements.Add(new RoleRequirement(AccessRoles.Admin)));
            options.AddPolicy(AccessPolicies.TreatmentUnit,p=>p.Requirements.Add(new RoleRequirement(AccessRoles.TreatmentUnit)));
            options.AddPolicy(AccessPolicies.CareStaff,p=>p.Requirements.Add(new RoleRequirement(AccessRoles.AuthenticatedCareUsers)));
        });
        services.AddSingleton<IAuthorizationHandler,RoleRequirementHandler>();
        return services;
    }
}
