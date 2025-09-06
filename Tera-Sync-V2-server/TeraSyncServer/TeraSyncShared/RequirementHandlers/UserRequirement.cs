using Microsoft.AspNetCore.Authorization;

namespace TeraSyncV2Shared.RequirementHandlers;

public class UserRequirement : IAuthorizationRequirement
{
    public UserRequirement(UserRequirements requirements)
    {
        Requirements = requirements;
    }

    public UserRequirements Requirements { get; }
}
