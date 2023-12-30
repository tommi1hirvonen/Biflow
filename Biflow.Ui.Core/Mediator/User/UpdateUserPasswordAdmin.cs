using MediatR;

namespace Biflow.Ui.Core;

public record UpdateUserPasswordAdminCommand(string Username, string Password) : IRequest;

internal class UpdateUserPasswordAdminCommandHandler(UserService userService) : IRequestHandler<UpdateUserPasswordAdminCommand>
{
    public async Task Handle(UpdateUserPasswordAdminCommand request, CancellationToken cancellationToken)
    {
        await userService.AdminUpdatePasswordAsync(request.Username, request.Password);
    }
}