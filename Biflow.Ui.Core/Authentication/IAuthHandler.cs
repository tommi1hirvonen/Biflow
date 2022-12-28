using System.Security.Authentication;

namespace Biflow.Ui.Core;

public interface IAuthHandler
{
    /// <summary>
    /// Authenticate user
    /// </summary>
    /// <param name="username"></param>
    /// <param name="password"></param>
    /// <returns>The role of the authenticated user</returns>
    /// <exception cref="InvalidCredentialException">If the provided credentials were not authroized</exception>
    /// <exception cref="AuthenticationException">If the resulting user role is unrecognized</exception>
    public async Task<string> AuthenticateUserAsync(string username, string password)
    {
        var role = await AuthenticateUserInternalAsync(username, password);
        if (role is null)
        {
            throw new InvalidCredentialException();
        }
        if (!new[] { "Admin", "Editor", "Operator", "Viewer" }.Contains(role))
        {
            throw new AuthenticationException($"Unrecognized user role {role}");
        }
        return role;
    }

    protected abstract Task<string?> AuthenticateUserInternalAsync(string username, string password);
}
