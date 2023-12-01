using Biflow.DataAccess.Models;
using FluentValidation;

namespace Biflow.Ui.Core;

public class StepValidator : AsyncAbstractValidator<Step>
{
    public StepValidator()
    {
        RuleFor(step => step)
            .Must(step => step.ExecutionConditionParameters.DistinctBy(p => p.ParameterName).Count() == step.ExecutionConditionParameters.Count)
            .WithMessage("Execution condition parameter names must be unique");
        RuleFor(step => step.DataObjects.Where(d => d.ReferenceType == DataObjectReferenceType.Source).Select(s => s.DataObject))
            .Must(HaveNoDuplicates)
            .WithMessage("Source object names must be unique");
        RuleFor(step => step.DataObjects.Where(d => d.ReferenceType == DataObjectReferenceType.Target).Select(t => t.DataObject))
            .Must(HaveNoDuplicates)
            .WithMessage("Target object names must be unique");
        RuleFor(step => step.ExecutionConditionExpression)
            .MustAsync(async (step, exp, ct) =>
            {
                try
                {
                    var result = await step.EvaluateExecutionConditionAsync();
                    return result is bool;
                }
                catch
                {
                    return true;
                }
            })
            .WithMessage("Incorrect execution condition expression return type")
            .When(step => !string.IsNullOrWhiteSpace(step.ExecutionConditionExpression.Expression));
        RuleFor(step => step.ExecutionConditionExpression)
            .MustAsync(async (step, exp, ct) =>
            {
                try
                {
                    await step.EvaluateExecutionConditionAsync();
                    return true;
                }
                catch
                {
                    return false;
                }
            })
            .WithMessage("Error validating execution condition expression")
            .When(step => !string.IsNullOrWhiteSpace(step.ExecutionConditionExpression.Expression));
        RuleForEach(step => step.ExecutionConditionParameters)
            .CustomAsync(async (param, context, ct) =>
            {
                try
                {
                    await param.EvaluateAsync();
                }
                catch
                {
                    context.AddFailure($"Error evaluating execution condition parameter '{param.DisplayName}'");
                }
            });
        RuleFor(step => step).SetInheritanceValidator(v =>
        {
            v.Add(new TabularStepValidator());
        });
        When(step => step is IHasStepParameters, () =>
        {
            // Built-in parameter names used in step expressions are reserved and cannot be used as user-defined parameter names.
            var reservedParameterNames = new string[]
            {
                ExpressionParameterNames.ExecutionId,
                ExpressionParameterNames.JobId,
                ExpressionParameterNames.StepId,
                ExpressionParameterNames.RetryAttemptIndex
            };
            foreach (var reservedName in reservedParameterNames)
            {
                RuleForEach(step => ((IHasStepParameters)step).StepParameters)
                    .Must(p => p.ExpressionParameters.All(ep => ep.ParameterName != reservedName))
                    .WithMessage((step, stepParam) => $"Reserved expression parameter name {reservedName} in parameter {stepParam.ParameterName}");
            }
        });
    }

    private static bool HaveNoDuplicates(IEnumerable<DataObject> objects)
    {
        var count = objects.Count();
        var distinctCount = objects
            .Select(o => o.ObjectUri)
            .Distinct()
            .Count();
        return count == distinctCount;
    }
}

file class TabularStepValidator : AbstractValidator<TabularStep>
{
    public TabularStepValidator()
    {
        RuleFor(step => step)
            .Must(step => string.IsNullOrEmpty(step.TabularPartitionName) || !string.IsNullOrEmpty(step.TabularPartitionName) && !string.IsNullOrEmpty(step.TabularTableName))
            .WithMessage("Table name is required if partition name has been defined");
    }
}