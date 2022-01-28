using EtlManager.DataAccess;
using EtlManager.DataAccess.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;

namespace EtlManager.Ui.Shared.JobDetails.StepEdit.StepEditModal;

public partial class FunctionStepEditModal : ParameterizedStepEditModal<FunctionStep>
{
    [Parameter] public IList<FunctionApp>? FunctionApps { get; set; }

    private FunctionSelectOffcanvas FunctionSelectOffcanvas { get; set; } = null!;

    internal override string FormId => "function_step_edit_form";

    private async Task OpenFunctionSelectOffcanvas() => await FunctionSelectOffcanvas.ShowAsync();

    private void OnFunctionSelected(string functionUrl)
    {
        Step.FunctionUrl = functionUrl;
    }

    protected override Task<FunctionStep> GetExistingStepAsync(EtlManagerContext context, Guid stepId) =>
        context.FunctionSteps
        .Include(step => step.StepParameters)
        .ThenInclude(p => p.JobParameter)
        .Include(step => step.Tags)
        .Include(step => step.Dependencies)
        .Include(step => step.Sources)
        .Include(step => step.Targets)
        .FirstAsync(step => step.StepId == stepId);

    protected override FunctionStep CreateNewStep(Job job) =>
        new()
        {
            JobId = job.JobId,
            RetryAttempts = 0,
            RetryIntervalMinutes = 0,
            IsEnabled = true,
            FunctionAppId = FunctionApps?.FirstOrDefault()?.FunctionAppId,
            Dependencies = new List<Dependency>(),
            Tags = new List<Tag>(),
            StepParameters = new List<StepParameterBase>()
        };
}
