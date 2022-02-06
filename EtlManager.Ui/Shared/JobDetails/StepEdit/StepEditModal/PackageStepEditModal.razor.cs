using EtlManager.DataAccess;
using EtlManager.DataAccess.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace EtlManager.Ui.Shared.JobDetails.StepEdit.StepEditModal;

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
            Targets = new List<SourceTargetObject>()
        };

    protected override Task<PackageStep> GetExistingStepAsync(EtlManagerContext context, Guid stepId) =>
        context.PackageSteps
        .Include(step => step.StepParameters)
        .ThenInclude(p => p.JobParameter)
        .Include(step => step.Tags)
        .Include(step => step.Dependencies)
        .Include(step => step.Sources)
        .Include(step => step.Targets)
        .FirstAsync(step => step.StepId == stepId);

    protected override void ResetDeletedEntities(EntityEntry entity)
    {
        if (entity.Entity is PackageStepParameter packageParam)
        {
            if (!Step.StepParameters.Contains(packageParam))
                Step.StepParameters.Add(packageParam);
        }
    }

    protected override void ResetAddedEntities(EntityEntry entity)
    {
        if (entity.Entity is PackageStepParameter packageParam)
        {
            if (Step.StepParameters.Contains(packageParam))
                Step.StepParameters.Remove(packageParam);
        }
    }

    protected override (bool Result, string? ErrorMessage) StepValidityCheck(Step step)
    {
        if (step is PackageStep package)
        {
            (var paramResult, var paramMessage) = ParametersCheck();
            if (!paramResult)
            {
                return (false, paramMessage);
            }
            else
            {
                foreach (var param in package.StepParameters)
                {
                    param.SetParameterValue();
                }
                return (true, null);
            }
        }
        else
        {
            return (false, "Not PackageStep");
        }
    }

    private (bool Result, string? Message) ParametersCheck()
    {
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
        Step.PackageFolderName = package.Folder;
        Step.PackageProjectName = package.Project;
        Step.PackageName = package.Package;
    }
}
