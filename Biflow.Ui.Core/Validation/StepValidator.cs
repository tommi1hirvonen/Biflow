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
        RuleFor(step => step).SetInheritanceValidator(v =>
        {
            v.Add(new TabularStepValidator());
        });
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