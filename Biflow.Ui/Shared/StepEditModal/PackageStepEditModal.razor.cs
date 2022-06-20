using Biflow.DataAccess;
using Biflow.DataAccess.Models;
using Biflow.Ui.Core;
using Biflow.Ui.Shared.StepEdit;
using Havit.Blazor.Components.Web.Bootstrap;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Biflow.Ui.Shared.StepEditModal;

public partial class PackageStepEditModal : ParameterizedStepEditModal<PackageStep>
{
    [Inject] private SqlServerHelperService SqlServerHelper { get; set; } = null!;

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

    private async Task ImportParametersAsync()
    {
        try
        {
            if (Step?.ConnectionId is null || Step.PackageFolderName is null || Step.PackageProjectName is null || Step.PackageName is null)
            {
                return;
            }
            var parameters = await SqlServerHelper.GetPackageParameters((Guid)Step.ConnectionId, Step.PackageFolderName, Step.PackageProjectName, Step.PackageName);
            if (!parameters.Any())
            {
                Messenger.AddInformation($"No parameters for package {Step.PackageFolderName}/{Step.PackageProjectName}/{Step.PackageName}.dtsx");
                return;
            }
            Step.StepParameters.Clear();
            foreach (var parameter in parameters)
            {
                Step.StepParameters.Add(new PackageStepParameter(parameter.ParameterLevel)
                {
                    ParameterName = parameter.ParameterName,
                    ParameterValueType = parameter.ParameterType,
                    ParameterValue = parameter.DefaultValue
                });
            }
        }
        catch (Exception ex)
        {
            Messenger.AddError("Error importing parameters", ex.Message);
        }
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
