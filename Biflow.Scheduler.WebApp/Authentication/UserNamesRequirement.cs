using Microsoft.AspNetCore.Authorization;

class UserNamesRequirement(params string[] userNames) : IAuthorizationRequirement
{
    public string[] UserNames { get; } = userNames;
}