using Biflow.DataAccess.Models;
using FluentValidation;

namespace Biflow.Ui.Core;

public class StepParametersValidator : AbstractValidator<IHasStepParameters>
{
    public StepParametersValidator()
    {
        // Validate step parameter name uniqueness using DisplayName to take into account ParameterLevel for PackageStepParameters.
        RuleFor(step => step)
            .Must(step => step.StepParameters.DistinctBy(p => p.DisplayName).Count() == step.StepParameters.Count)
            .WithMessage("Parameter names must be unique");
        RuleForEach(step => step.StepParameters)
            .Must(parameters => parameters.ExpressionParameters.DistinctBy(p => p.ParameterName).Count() == parameters.ExpressionParameters.Count)
            .WithMessage("Expression parameter names must be unique");
    }
}