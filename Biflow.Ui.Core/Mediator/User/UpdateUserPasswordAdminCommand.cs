using MediatR;

namespace Biflow.Ui.Core;

public record UpdateUserPasswordAdminCommand(string Username, string Password) : IRequest;