namespace Biflow.Ui.Authentication;

internal class BuiltInAuthHandler(IMediator mediator) : IAuthHandler
{
    public async Task<IReadOnlyList<string>> AuthenticateUserInternalAsync(string username, string password)
    {
        var response = await mediator.SendAsync(new UserAuthenticateQuery(username, password));
        return response.Roles;
    }
}
