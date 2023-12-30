using MediatR;

namespace Biflow.Ui.Core;

public record UpdateUserPasswordCommand(string Username, string OldPassword, string NewPassword) : IRequest;