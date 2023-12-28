using Biflow.DataAccess.Models;
using MediatR;

namespace Biflow.Ui.Core;

public record DeleteStepsRequest(params Guid[] StepIds) : IRequest
{
    public DeleteStepsRequest(IEnumerable<Step> steps) : this(steps.Select(s => s.StepId).ToArray()) { }
}
