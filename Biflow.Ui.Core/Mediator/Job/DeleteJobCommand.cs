using MediatR;

namespace Biflow.Ui.Core;

public record DeleteJobCommand(Guid JobId) : IRequest;
