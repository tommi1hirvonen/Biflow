using Biflow.DataAccess.Models;
using FluentValidation;

namespace Biflow.Ui.Core;

public class StepParametersValidator : AbstractValidator<IHasStepParameters>
{
    public StepParametersValidator()
    {
        RuleFor(step => step)
            .Must(step => step.StepParameters.DistinctBy(p => p.DisplayName).Count() == step.StepParameters.Count)
            .WithMessage("Parameter names must be unique");
    }
}