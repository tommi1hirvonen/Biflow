using Biflow.Ui.Shared.StepEdit;

namespace Biflow.Ui.Shared.StepEditModal;

public partial class DatasetStepEditModal(
    ITokenService tokenService,
    ToasterService toaster,
    IDbContextFactory<AppDbContext> dbContextFactory)
    : StepEditModal<DatasetStep>(toaster, dbContextFactory)
{
    private readonly ITokenService _tokenService = tokenService;

    internal override string FormId => "dataset_step_edit_form";

    private DatasetSelectOffcanvas? _datasetSelectOffcanvas;
    private string? _datasetGroupName;
    private string? _datasetName;

    private void OnDatasetSelected(Dataset dataset)
    {
        ArgumentNullException.ThrowIfNull(Step);
        (Step.DatasetGroupId, _datasetGroupName, Step.DatasetId, _datasetName) = dataset;
    }

    private Task OpenDatasetSelectOffcanvas() => _datasetSelectOffcanvas.LetAsync(x => x.ShowAsync(Step?.AzureCredentialId));

    protected override async Task OnModalShownAsync(DatasetStep step)
    {
        try
        {
            var azureCredential = AzureCredentials.First(a => a.AzureCredentialId == step.AzureCredentialId);
            var datasetClient = azureCredential.CreateDatasetClient(_tokenService);
            (_datasetGroupName, _datasetName) = azureCredential switch
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
            (_datasetGroupName, _datasetName) = ("", "");
        }
        finally
        {
            StateHasChanged();
        }
    }

    protected override async Task<DatasetStep> GetExistingStepAsync(AppDbContext context, Guid stepId)
    {
        (_datasetGroupName, _datasetName) = (null, null);
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
        (_datasetGroupName, _datasetName) = ("", "");
        return new()
        {
            JobId = job.JobId,
            Job = job,
            RetryAttempts = 0,
            RetryIntervalMinutes = 0,
            AzureCredentialId = AzureCredentials.First().AzureCredentialId
        };
    }
}
