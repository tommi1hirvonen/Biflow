using MediatR;

namespace Biflow.Ui.Core;

internal class UpdateUserPasswordCommandHandler(UserService userService) : IRequestHandler<UpdateUserPasswordCommand>
{
    public async Task Handle(UpdateUserPasswordCommand request, CancellationToken cancellationToken)
    {
        await userService.UpdatePasswordAsync(request.Username, request.OldPassword, request.NewPassword);
    }
}
