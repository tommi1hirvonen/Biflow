namespace Biflow.Ui.Core;

internal class BuiltInAuthHandler(UserService users) : IAuthHandler
{
    private readonly UserService _users = users;

    public async Task<IEnumerable<string>> AuthenticateUserInternalAsync(string username, string password)
    {
        return await _users.AuthenticateUserAsync(username, password);
    }
}
