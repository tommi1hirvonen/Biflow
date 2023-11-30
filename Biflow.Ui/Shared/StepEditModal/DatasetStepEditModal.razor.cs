using Biflow.DataAccess;
using Biflow.DataAccess.Models;
using Biflow.Ui.Core;
using Biflow.Ui.Shared.StepEdit;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;

namespace Biflow.Ui.Shared.StepEditModal;

public partial class DatasetStepEditModal : StepEditModal<DatasetStep>
{
    [Inject] private ITokenService TokenService { get; set; } = null!;

    [Parameter] public IList<AppRegistration>? AppRegistrations { get; set; }

    internal override string FormId => "dataset_step_edit_form";

    private DatasetSelectOffcanvas? datasetSelectOffcanvas;
    private string? datasetGroupName;
    private string? datasetName;

    private void OnDatasetSelected(Dataset dataset)
    {
        ArgumentNullException.ThrowIfNull(Step);
        (Step.DatasetGroupId, datasetGroupName, Step.DatasetId, datasetName) = dataset;
    }

    private Task OpenDatasetSelectOffcanvas() => datasetSelectOffcanvas.LetAsync(x => x.ShowAsync());

    protected override async Task OnModalShownAsync(DatasetStep step)
    {
        try
        {
            var appRegistration = AppRegistrations?.FirstOrDefault(a => a.AppRegistrationId == step.AppRegistrationId);
            (datasetGroupName, datasetName) = (appRegistration, step) switch
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
            (datasetGroupName, datasetName) = ("", "");
        }
        finally
        {
            StateHasChanged();
        }
    }

    protected override async Task<DatasetStep> GetExistingStepAsync(AppDbContext context, Guid stepId)
    {
        (datasetGroupName, datasetName) = (null, null);
        var step = await context.DatasetSteps
            .Include(step => step.Job)
            .Include(step => step.Tags)
            .Include(step => step.Dependencies)
            .Include(step => step.Sources)
            .ThenInclude(s => s.DataObject)
            .Include(step => step.Targets)
            .ThenInclude(t => t.DataObject)
            .Include(step => step.ExecutionConditionParameters)
            .FirstAsync(step => step.StepId == stepId);
        
        return step;
    }

    protected override DatasetStep CreateNewStep(Job job)
    {
        (datasetGroupName, datasetName) = ("", "");
        return new(job.JobId)
        {
            Job = job,
            RetryAttempts = 0,
            RetryIntervalMinutes = 0,
            AppRegistrationId = AppRegistrations?.FirstOrDefault()?.AppRegistrationId,
            Dependencies = new List<Dependency>(),
            Tags = new List<Tag>(),
            Sources = new List<StepSource>(),
            Targets = new List<StepTarget>(),
            ExecutionConditionParameters = new List<ExecutionConditionParameter>()
        };
    }
}
