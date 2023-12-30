using MediatR;

namespace Biflow.Ui.Core.Mediator.User;

internal class UpdateUserPasswordAdminCommandHandler(UserService userService) : IRequestHandler<UpdateUserPasswordAdminCommand>
{
    public async Task Handle(UpdateUserPasswordAdminCommand request, CancellationToken cancellationToken)
    {
        await userService.AdminUpdatePasswordAsync(request.Username, request.Password);
    }
}
