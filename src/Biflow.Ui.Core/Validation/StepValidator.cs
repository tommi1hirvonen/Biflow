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
            .MustAsync(async (step, _, _) =>
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
            .MustAsync(async (step, _, _) =>
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
            .CustomAsync(async (param, context, _) =>
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
            v.Add(new DbtStepValidator());
            v.Add(new DatabricksStepValidator());
            v.Add(new FunctionStepValidator());
            v.Add(new HttpStepValidator());
            v.Add(new ExeStepValidator());
        });
        When(step => step is IHasStepParameters, () =>
        {
            // Built-in parameter names used in step expressions are reserved and cannot be used as user-defined parameter names.
            var reservedParameterNames = new[]
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
                    .WithMessage((_, stepParam) => $"Reserved expression parameter name {reservedName} in parameter {stepParam.ParameterName}");
            }
        });
    }

    private static bool HaveNoDuplicates(IEnumerable<DataObject> objects)
    {
        var a = objects.ToArray();
        var distinctCount = a
            .Select(o => o.ObjectUri)
            .Distinct()
            .Count();
        return a.Length == distinctCount;
    }
}

file class ExeStepValidator : AbstractValidator<ExeStep>
{
    public ExeStepValidator()
    {
        RuleFor(step => step)
            .Must(step => step.RunAsCredentialId is null || step.ProxyId is null)
            .WithMessage("Impersonation is not supported when using proxies to run executables");
    }
}

file class FunctionStepValidator : AbstractValidator<FunctionStep>
{
    public FunctionStepValidator()
    {
        RuleFor(step => step)
            .Must(step => step.FunctionAppId is not null || !string.IsNullOrEmpty(step.FunctionKey))
            .WithMessage("Either the function app or the function key property must be set");
    }
}

file class HttpStepValidator : AbstractValidator<HttpStep>
{
    public HttpStepValidator()
    {
        RuleFor(step => step.Headers)
            .Must(headers => headers.Select(h => h.Key).Distinct().Count() == headers.Count)
            .WithMessage("The header keys must be unique");
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

file class DbtStepValidator : AbstractValidator<DbtStep>
{
    public DbtStepValidator()
    {
        RuleFor(step => step.DbtJob.Id)
            .NotEmpty()
            .WithMessage("No dbt job was set");
    }
}

file class DatabricksStepValidator : AbstractValidator<DatabricksStep>
{
    public DatabricksStepValidator()
    {
        RuleFor(step => step.DatabricksStepSettings)
            .SetValidator(new DatabricksStepSettingsValidator());
    }
}

file class DatabricksStepSettingsValidator : AbstractValidator<DatabricksStepSettings>
{
    public DatabricksStepSettingsValidator()
    {
        RuleFor(settings => settings)
            .SetInheritanceValidator(v =>
            {
                v.Add(new DbNotebookStepSettingsValidator());
                v.Add(new DbSqlNotebookStepSettingsValidator());
                v.Add(new DbPythonFileStepSettingsValidator());
                v.Add(new DbPipelineStepSettingsValidator());
                v.Add(new DbJobStepSettingsValidator());
            });
    }
}

file class DbNotebookStepSettingsValidator : AbstractValidator<DbNotebookStepSettings>
{
    public DbNotebookStepSettingsValidator()
    {
        RuleFor(settings => settings.NotebookPath).NotEmpty();
        RuleFor(settings => settings.ClusterConfiguration)
            .SetInheritanceValidator(v =>
            {
                v.Add(new ExistingClusterValidator());
                v.Add(new NewClusterValidator());
            });
    }
}

file class DbSqlNotebookStepSettingsValidator : AbstractValidator<DbSqlNotebookStepSettings>
{
	public DbSqlNotebookStepSettingsValidator()
	{
		RuleFor(settings => settings.NotebookPath).NotEmpty();
		RuleFor(settings => settings.WarehouseId).NotEmpty();
	}
}

file class DbPythonFileStepSettingsValidator : AbstractValidator<DbPythonFileStepSettings>
{
    public DbPythonFileStepSettingsValidator()
    {
        RuleFor(settings => settings.FilePath).NotEmpty();
        RuleFor(settings => settings.ClusterConfiguration)
            .SetInheritanceValidator(v =>
            {
                v.Add(new ExistingClusterValidator());
                v.Add(new NewClusterValidator());
            });
    }
}

file class DbPipelineStepSettingsValidator : AbstractValidator<DbPipelineStepSettings>
{
    public DbPipelineStepSettingsValidator()
    {
        RuleFor(settings => settings.PipelineId).NotEmpty();
    }
}

file class DbJobStepSettingsValidator : AbstractValidator<DbJobStepSettings>
{
    public DbJobStepSettingsValidator()
    {
        RuleFor(settings => settings.JobId).NotEmpty();
    }
}

file class ExistingClusterValidator : AbstractValidator<ExistingClusterConfiguration>
{
    public ExistingClusterValidator()
    {
        RuleFor(c => c.ClusterId).NotEmpty();
    }
}

file class NewClusterValidator : AbstractValidator<NewClusterConfiguration>
{
    public NewClusterValidator()
    {
        RuleFor(c => c.RuntimeVersion).NotEmpty();
        RuleFor(c => c.NodeTypeId).NotEmpty();
        RuleFor(c => c.ClusterMode)
            .SetInheritanceValidator(v =>
            {
                v.Add(new FixedClusterValidator());
                v.Add(new AutoClusterValidator());
            });
    }
}

file class FixedClusterValidator : AbstractValidator<FixedMultiNodeClusterConfiguration>
{
    public FixedClusterValidator()
    {
        RuleFor(c => c.NumberOfWorkers).GreaterThan(0);
    }
}

file class AutoClusterValidator : AbstractValidator<AutoscaleMultiNodeClusterConfiguration>
{
    public AutoClusterValidator()
    {
        RuleFor(c => c.MaximumWorkers).GreaterThan(c => c.MinimumWorkers);
        RuleFor(c => c.MinimumWorkers).GreaterThan(0);
    }
}