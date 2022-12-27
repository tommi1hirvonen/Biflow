namespace Biflow.Ui.Core;

internal class BuiltInAuthHandler : IAuthHandler
{
    private readonly DbHelperService _dbHelper;

    public BuiltInAuthHandler(DbHelperService dbHelper)
    {
        _dbHelper = dbHelper;
    }

    public async Task<string?> AuthenticateUserInternalAsync(string username, string password)
    {
        return await _dbHelper.AuthenticateUserAsync(username, password);
    }
}
