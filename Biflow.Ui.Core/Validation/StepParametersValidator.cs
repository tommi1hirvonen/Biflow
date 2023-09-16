using Biflow.DataAccess.Models;
using FluentValidation;

namespace Biflow.Ui.Core;

public class StepParametersValidator : AsyncAbstractValidator<IHasStepParameters>
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
        RuleForEach(step => step.StepParameters)
            .CustomAsync(async (param, context, ct) =>
            {
                try
                {
                    await param.EvaluateAsync();
                }
                catch
                {
                    context.AddFailure($"Error evaluating parameter '{param.DisplayName}'");
                }
            });
    }
}