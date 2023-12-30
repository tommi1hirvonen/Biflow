using MediatR;

namespace Biflow.Ui.Core;

public record DeleteConnectionCommand(Guid ConnectionId) : IRequest;