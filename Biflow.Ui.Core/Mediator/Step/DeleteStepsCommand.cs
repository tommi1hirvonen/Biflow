using Biflow.DataAccess.Models;
using MediatR;

namespace Biflow.Ui.Core;

public record DeleteStepsCommand(params Guid[] StepIds) : IRequest
{
    public DeleteStepsCommand(IEnumerable<Step> steps) : this(steps.Select(s => s.StepId).ToArray()) { }
}
