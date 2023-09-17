using Microsoft.AspNetCore.Authorization;

class UserNamesRequirement : IAuthorizationRequirement
{
    public string[] UserNames { get; }

    public UserNamesRequirement(params string[] userNames)
    {
        UserNames = userNames;
    }
}