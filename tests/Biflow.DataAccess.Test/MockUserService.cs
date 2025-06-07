namespace Biflow.DataAccess.Test;

internal class MockUserService(string username, string role) : IUserService
{
    public string Username => username;

    public IEnumerable<string>? Roles { get; } = [role];
}
