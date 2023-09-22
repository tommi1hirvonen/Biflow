namespace Biflow.Ui.Core;

internal class BuiltInAuthHandler : IAuthHandler
{
    private readonly UserService _users;

    public BuiltInAuthHandler(UserService users)
    {
        _users = users;
    }

    public async Task<IEnumerable<string>> AuthenticateUserInternalAsync(string username, string password)
    {
        return await _users.AuthenticateUserAsync(username, password);
    }
}
