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
    /// <exception cref="InvalidCredentialException">If the provided credentials were not authorized</exception>
    /// <exception cref="AuthenticationException">If the resulting user role is unrecognized</exception>
    public async Task<IEnumerable<string>> AuthenticateUserAsync(string username, string password)
    {
        var roles = await AuthenticateUserInternalAsync(username, password);
        
        if (!roles.Any())
        {
            throw new InvalidCredentialException();
        }
        return roles;
    }

    protected Task<IEnumerable<string>> AuthenticateUserInternalAsync(string username, string password);
}
