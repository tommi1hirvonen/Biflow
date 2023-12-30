using MediatR;

namespace Biflow.Ui.Core;

public record DeleteExecutionCommand(Guid ExecutionId) : IRequest;