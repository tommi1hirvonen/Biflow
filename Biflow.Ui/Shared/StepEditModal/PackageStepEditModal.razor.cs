using Biflow.DataAccess;
using Biflow.DataAccess.Models;
using Biflow.Ui.Core;
using Biflow.Ui.Shared.StepEdit;
using Havit.Blazor.Components.Web.Bootstrap;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;

namespace Biflow.Ui.Shared.StepEditModal;

public partial class PackageStepEditModal : StepEditModal<PackageStep>
{
    [Inject] private SqlServerHelperService SqlServerHelper { get; set; } = null!;

    private PackageSelectOffcanvas? PackageSelectOffcanvas { get; set; }

    internal override string FormId => "package_step_edit_form";

    protected override PackageStep CreateNewStep(Job job) =>
        new()
        {
            JobId = job.JobId,
            Job = job,
            RetryAttempts = 0,
            RetryIntervalMinutes = 0,
            IsEnabled = true,
            ConnectionId = Connections?.FirstOrDefault()?.ConnectionId,
            StepParameters = new List<PackageStepParameter>(),
            Dependencies = new List<Dependency>(),
            Tags = new List<Tag>(),
            Sources = new List<DataObject>(),
            Targets = new List<DataObject>(),
            ExecutionConditionParameters = new List<ExecutionConditionParameter>()
        };

    protected override Task<PackageStep> GetExistingStepAsync(BiflowContext context, Guid stepId) =>
        context.PackageSteps
        .Include(step => step.Job)
        .ThenInclude(job => job.JobParameters)
        .Include(step => step.StepParameters)
        .ThenInclude(p => p.InheritFromJobParameter)
        .Include(step => step.StepParameters)
        .ThenInclude(p => p.ExpressionParameters)
        .Include(step => step.Tags)
        .Include(step => step.Dependencies)
        .Include(step => step.Sources)
        .Include(step => step.Targets)
        .Include(step => step.ExecutionConditionParameters)
        .FirstAsync(step => step.StepId == stepId);

    protected override (bool Result, string? Message) ParametersCheck()
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
            var currentParam = parameters[i];
            var nextParam = parameters[i + 1];
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

    private Task OpenPackageSelectOffcanvas() => PackageSelectOffcanvas.LetAsync(x => x.ShowAsync());

    private void OnPackageSelected(PackageSelectedResponse package)
    {
        ArgumentNullException.ThrowIfNull(Step);
        (Step.PackageFolderName, Step.PackageProjectName, Step.PackageName) = package;
    }
}
