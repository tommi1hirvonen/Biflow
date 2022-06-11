using Biflow.DataAccess;
using Biflow.DataAccess.Models;
using Biflow.Ui.Shared.StepEdit;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Biflow.Ui.Shared.StepEditModal;

public partial class PackageStepEditModal : ParameterizedStepEditModal<PackageStep>
{

    private PackageSelectOffcanvas PackageSelectOffcanvas { get; set; } = null!;

    internal override string FormId => "package_step_edit_form";

    protected override PackageStep CreateNewStep(Job job) =>
        new()
        {
            JobId = job.JobId,
            RetryAttempts = 0,
            RetryIntervalMinutes = 0,
            IsEnabled = true,
            ConnectionId = Connections?.FirstOrDefault()?.ConnectionId,
            StepParameters = new List<StepParameterBase>(),
            Dependencies = new List<Dependency>(),
            Tags = new List<Tag>(),
            Sources = new List<SourceTargetObject>(),
            Targets = new List<SourceTargetObject>(),
            ExecutionConditionParameters = new List<ExecutionConditionParameter>()
        };

    protected override Task<PackageStep> GetExistingStepAsync(BiflowContext context, Guid stepId) =>
        context.PackageSteps
        .Include(step => step.StepParameters)
        .ThenInclude(p => p.JobParameter)
        .Include(step => step.Tags)
        .Include(step => step.Dependencies)
        .Include(step => step.Sources)
        .Include(step => step.Targets)
        .Include(step => step.ExecutionConditionParameters)
        .FirstAsync(step => step.StepId == stepId);

    protected override void ResetDeletedEntities(EntityEntry entity)
    {
        ArgumentNullException.ThrowIfNull(Step);
        base.ResetDeletedEntities(entity);
        if (entity.Entity is PackageStepParameter packageParam)
        {
            if (!Step.StepParameters.Contains(packageParam))
                Step.StepParameters.Add(packageParam);
        }
    }

    protected override void ResetAddedEntities(EntityEntry entity)
    {
        ArgumentNullException.ThrowIfNull(Step);
        base.ResetAddedEntities(entity);
        if (entity.Entity is PackageStepParameter packageParam)
        {
            if (Step.StepParameters.Contains(packageParam))
                Step.StepParameters.Remove(packageParam);
        }
    }

    protected override (bool Result, string? ErrorMessage) StepValidityCheck(Step step)
    {
        (var paramResultBase, var paramMessageBase) = base.StepValidityCheck(step);
        if (!paramResultBase)
        {
            return (false, paramMessageBase);
        }
        if (step is PackageStep package)
        {
            (var paramResult, var paramMessage) = ParametersCheck();
            if (!paramResult)
            {
                return (false, paramMessage);
            }
            foreach (var param in package.StepParameters)
            {
                param.SetParameterValue();
            }
            return (true, null);
        }
        else
        {
            return (false, "Not PackageStep");
        }
    }

    private (bool Result, string? Message) ParametersCheck()
    {
        ArgumentNullException.ThrowIfNull(Step);
        var parameters = Step.StepParameters.OrderBy(param => param.ParameterName).ToList();
        foreach (var param in parameters)
        {
            if (string.IsNullOrEmpty(param.ParameterName))
            {
                return (false, "Parameter name cannot be empty");
            }
        }
        for (var i = 0; i < parameters.Count - 1; i++)
        {
            var currentParam = (PackageStepParameter)parameters[i];
            var nextParam = (PackageStepParameter)parameters[i + 1];
            if (nextParam.ParameterName == currentParam.ParameterName
                && nextParam.ParameterLevel == currentParam.ParameterLevel)
            {
                return (false, "Duplicate parameter names");
            }
        }

        return (true, null);
    }

    private Task OpenPackageSelectOffcanvas() => PackageSelectOffcanvas.ShowAsync();

    private void OnPackageSelected((string Folder, string Project, string Package) package)
    {
        ArgumentNullException.ThrowIfNull(Step);
        Step.PackageFolderName = package.Folder;
        Step.PackageProjectName = package.Project;
        Step.PackageName = package.Package;
    }
}
