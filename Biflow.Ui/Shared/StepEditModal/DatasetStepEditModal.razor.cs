using Biflow.Ui.Shared.StepEdit;

namespace Biflow.Ui.Shared.StepEditModal;

public partial class DatasetStepEditModal : StepEditModal<DatasetStep>
{
    [Inject] private ITokenService TokenService { get; set; } = null!;

    internal override string FormId => "dataset_step_edit_form";

    private DatasetSelectOffcanvas? datasetSelectOffcanvas;
    private string? datasetGroupName;
    private string? datasetName;

    private void OnDatasetSelected(Dataset dataset)
    {
        ArgumentNullException.ThrowIfNull(Step);
        (Step.DatasetGroupId, datasetGroupName, Step.DatasetId, datasetName) = dataset;
    }

    private Task OpenDatasetSelectOffcanvas() => datasetSelectOffcanvas.LetAsync(x => x.ShowAsync(Step?.AppRegistrationId));

    protected override async Task OnModalShownAsync(DatasetStep step)
    {
        try
        {
            var appRegistration = AppRegistrations.First(a => a.AppRegistrationId == step.AppRegistrationId);
            var datasetClient = appRegistration.CreateDatasetClient(TokenService);
            (datasetGroupName, datasetName) = appRegistration switch
            {
                not null => (
                    await datasetClient.GetGroupNameAsync(step.DatasetGroupId),
                    await datasetClient.GetDatasetNameAsync(step.DatasetGroupId, step.DatasetId)
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
            .Include(step => step.DataObjects)
            .ThenInclude(s => s.DataObject)
            .Include(step => step.ExecutionConditionParameters)
            .FirstAsync(step => step.StepId == stepId);
        
        return step;
    }

    protected override DatasetStep CreateNewStep(Job job)
    {
        (datasetGroupName, datasetName) = ("", "");
        return new()
        {
            JobId = job.JobId,
            Job = job,
            RetryAttempts = 0,
            RetryIntervalMinutes = 0,
            AppRegistrationId = AppRegistrations.First().AppRegistrationId
        };
    }
}
