using Biflow.Ui.Shared.StepEdit;

namespace Biflow.Ui.Shared.StepEditModal;

public partial class DatasetStepEditModal(
    ITokenService tokenService,
    ToasterService toaster,
    IDbContextFactory<AppDbContext> dbContextFactory)
    : StepEditModal<DatasetStep>(toaster, dbContextFactory)
{
    internal override string FormId => "dataset_step_edit_form";

    private DatasetSelectOffcanvas? _datasetSelectOffcanvas;
    private bool _loading;

    protected override DatasetStep CreateNewStep(Job job) =>
        new()
        {
            JobId = job.JobId,
            Job = job,
            RetryAttempts = 0,
            RetryIntervalMinutes = 0,
            AzureCredentialId = AzureCredentials.First().AzureCredentialId
        };
    
    protected override Task<DatasetStep> GetExistingStepAsync(AppDbContext context, Guid stepId) =>
        context.DatasetSteps
            .Include(step => step.Job)
            .Include(step => step.Tags)
            .Include(step => step.Dependencies)
            .Include(step => step.DataObjects)
            .ThenInclude(s => s.DataObject)
            .Include(step => step.ExecutionConditionParameters)
            .FirstAsync(step => step.StepId == stepId);

    protected override async Task OnModalShownAsync(DatasetStep step)
    {
        try
        {
            _loading = true;
            StateHasChanged();
            var azureCredential = AzureCredentials.First(a => a.AzureCredentialId == step.AzureCredentialId);
            var datasetClient = azureCredential.CreateDatasetClient(tokenService);
            (step.WorkspaceName, step.DatasetName) =
                (await datasetClient.GetWorkspaceNameAsync(step.WorkspaceId),
                    await datasetClient.GetDatasetNameAsync(step.WorkspaceId, step.DatasetId));
        }
        catch (Exception ex)
        {
            Toaster.AddError("Error getting names from API", ex.Message);
        }
        finally
        {
            _loading = false;
            StateHasChanged();
        }
    }
    
    private Task OpenDatasetSelectOffcanvas() =>
        _datasetSelectOffcanvas.LetAsync(x => x.ShowAsync(Step?.AzureCredentialId));

    private void OnDatasetSelected(Dataset dataset)
    {
        ArgumentNullException.ThrowIfNull(Step);
        (Step.WorkspaceId, Step.WorkspaceName, Step.DatasetId, Step.DatasetName) = dataset;
    }
}
