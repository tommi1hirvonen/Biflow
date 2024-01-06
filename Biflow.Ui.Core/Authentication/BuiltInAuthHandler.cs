using MediatR;

namespace Biflow.Ui.Core;

internal class BuiltInAuthHandler(IMediator mediator) : IAuthHandler
{
    public async Task<IEnumerable<string>> AuthenticateUserInternalAsync(string username, string password)
    {
        var response = await mediator.Send(new UserAuthenticateQuery(username, password));
        return response.Roles;
    }
}
