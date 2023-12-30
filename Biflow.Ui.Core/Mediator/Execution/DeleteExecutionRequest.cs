using MediatR;

namespace Biflow.Ui.Core;

public record DeleteExecutionRequest(Guid ExecutionId) : IRequest;