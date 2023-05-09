using Biflow.DataAccess.Models;
using FluentValidation;

namespace Biflow.Ui.Core;

public class StepValidator : AbstractValidator<Step>
{
    public StepValidator()
    {
        RuleFor(step => step)
            .Must(step => step.ExecutionConditionParameters.DistinctBy(p => p.ParameterName).Count() == step.ExecutionConditionParameters.Count)
            .WithMessage("Execution condition parameter names must be unique");
        RuleFor(step => step.Sources)
            .Must(HaveNoDuplicates)
            .WithMessage("Source object names must be unique");
        RuleFor(step => step.Targets)
            .Must(HaveNoDuplicates)
            .WithMessage("Target object names must be unique");
        RuleFor(step => step).SetInheritanceValidator(v =>
        {
            v.Add(new TabularStepValidator());
        });
        When(step => step is IHasStepParameters, () =>
        {
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
        var ordered = objects
            .OrderBy(x => x.ServerName)
            .ThenBy(x => x.DatabaseName)
            .ThenBy(x => x.SchemaName)
            .ThenBy(x => x.ObjectName)
            .ToList();
        for (int i = 0; i < ordered.Count - 1; i++)
        {
            var current = ordered.ElementAt(i);
            var next = ordered.ElementAt(i + 1);
            if (current.NamesEqual(next))
            {
                return false;
            }
        }
        return true;
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