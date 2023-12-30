using MediatR;

namespace Biflow.Ui.Core;

public record DeleteStepTagCommand(Guid StepId, Guid TagId) : IRequest;