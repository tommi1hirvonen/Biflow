using MediatR;

namespace Biflow.Ui.Core;

public record DeleteStepTagRequest(Guid StepId, Guid TagId) : IRequest;