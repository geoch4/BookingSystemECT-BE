// Backend purpose: Defines authorization behavior for RoleRequirement.
using System.Collections.Generic;
using Microsoft.AspNetCore.Authorization;


namespace BookingSystem.Application.Authorization;

public class RoleRequirement : IAuthorizationRequirement
{
    public IReadOnlyCollection<string> RequiredRoles { get; }

    public RoleRequirement(params string[] requiredRoles)
    {
        RequiredRoles = requiredRoles;
    }
}
