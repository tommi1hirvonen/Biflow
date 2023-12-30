using MediatR;

namespace Biflow.Ui.Core;

public record DeleteUserCommand(Guid UserId) : IRequest;