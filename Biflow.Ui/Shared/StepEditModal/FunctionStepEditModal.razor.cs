using Biflow.DataAccess;
using Biflow.DataAccess.Models;
using Biflow.Ui.Core;
using Biflow.Ui.Shared.StepEdit;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;

namespace Biflow.Ui.Shared.StepEditModal;

public partial class FunctionStepEditModal : StepEditModal<FunctionStep>
{
    [Parameter] public IList<FunctionApp>? FunctionApps { get; set; }

    private FunctionSelectOffcanvas? FunctionSelectOffcanvas { get; set; }

    internal override string FormId => "function_step_edit_form";

    private Task OpenFunctionSelectOffcanvas()
    {
        ArgumentNullException.ThrowIfNull(Step?.FunctionAppId);
        return FunctionSelectOffcanvas.LetAsync(x => x.ShowAsync((Guid)Step.FunctionAppId));
    }

    private void OnFunctionSelected(string functionUrl)
    {
        ArgumentNullException.ThrowIfNull(Step);
        Step.FunctionUrl = functionUrl;
    }

    protected override Task<FunctionStep> GetExistingStepAsync(BiflowContext context, Guid stepId) =>
        context.FunctionSteps
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

    protected override FunctionStep CreateNewStep(Job job) =>
        new(job.JobId)
        {
            Job = job,
            RetryAttempts = 0,
            RetryIntervalMinutes = 0,
            FunctionAppId = FunctionApps?.FirstOrDefault()?.FunctionAppId,
            Dependencies = new List<Dependency>(),
            Tags = new List<Tag>(),
            StepParameters = new List<FunctionStepParameter>(),
            Sources = new List<DataObject>(),
            Targets = new List<DataObject>(),
            ExecutionConditionParameters = new List<ExecutionConditionParameter>()
        };
}
