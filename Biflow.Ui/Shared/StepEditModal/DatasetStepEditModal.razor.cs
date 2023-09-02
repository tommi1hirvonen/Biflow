using Biflow.DataAccess;
using Biflow.DataAccess.Models;
using Biflow.Ui.Core;
using Biflow.Ui.Shared.StepEdit;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;

namespace Biflow.Ui.Shared.StepEditModal;

public partial class DatasetStepEditModal : StepEditModal<DatasetStep>
{
    [Parameter] public IList<AppRegistration>? AppRegistrations { get; set; }

    [Inject] private ITokenService TokenService { get; set; } = null!;

    private DatasetSelectOffcanvas? DatasetSelectOffcanvas { get; set; }

    internal override string FormId => "dataset_step_edit_form";

    private Task OpenDatasetSelectOffcanvas() => DatasetSelectOffcanvas.LetAsync(x => x.ShowAsync());

    private string? DatasetGroupName { get; set; }

    private string? DatasetName { get; set; }

    private void OnDatasetSelected(DatasetSelectedResponse dataset)
    {
        ArgumentNullException.ThrowIfNull(Step);
        (Step.DatasetGroupId, DatasetGroupName, Step.DatasetId, DatasetName) = dataset;
    }

    protected override async Task OnModalShownAsync(DatasetStep step)
    {
        try
        {
            var appRegistration = AppRegistrations?.FirstOrDefault(a => a.AppRegistrationId == step.AppRegistrationId);
            (DatasetGroupName, DatasetName) = (appRegistration, step) switch
            {
                (not null, { DatasetGroupId: not null, DatasetId: not null }) => (
                    await appRegistration.GetGroupNameAsync(step.DatasetGroupId, TokenService),
                    await appRegistration.GetDatasetNameAsync(step.DatasetGroupId, step.DatasetId, TokenService)
                    ),
                _ => ("", "")
            };
        }
        catch
        {
            (DatasetGroupName, DatasetName) = ("", "");
        }
    }

    protected override async Task<DatasetStep> GetExistingStepAsync(BiflowContext context, Guid stepId)
    {
        (DatasetGroupName, DatasetName) = (null, null);
        var step = await context.DatasetSteps
            .Include(step => step.Job)
            .Include(step => step.Tags)
            .Include(step => step.Dependencies)
            .Include(step => step.Sources)
            .Include(step => step.Targets)
            .Include(step => step.ExecutionConditionParameters)
            .FirstAsync(step => step.StepId == stepId);
        
        return step;
    }

    protected override DatasetStep CreateNewStep(Job job)
    {
        (DatasetGroupName, DatasetName) = ("", "");
        return new(job.JobId)
        {
            Job = job,
            RetryAttempts = 0,
            RetryIntervalMinutes = 0,
            AppRegistrationId = AppRegistrations?.FirstOrDefault()?.AppRegistrationId,
            Dependencies = new List<Dependency>(),
            Tags = new List<Tag>(),
            Sources = new List<DataObject>(),
            Targets = new List<DataObject>(),
            ExecutionConditionParameters = new List<ExecutionConditionParameter>()
        };
    }
}
