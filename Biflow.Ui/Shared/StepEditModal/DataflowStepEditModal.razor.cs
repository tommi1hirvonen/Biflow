using Biflow.Ui.Shared.StepEdit;

namespace Biflow.Ui.Shared.StepEditModal;

public partial class DataflowStepEditModal(
    ITokenService tokenService,
    IHttpClientFactory httpClientFactory,
    ToasterService toaster,
    IDbContextFactory<AppDbContext> dbContextFactory)
    : StepEditModal<DataflowStep>(toaster, dbContextFactory)
{
    internal override string FormId => "dataflow_step_edit_form";
    
    private DataflowSelectOffcanvas? _dataflowSelectOffcanvas;
    private bool _loading;

    protected override DataflowStep CreateNewStep(Job job) =>
        new()
        {
            JobId = job.JobId,
            Job = job,
            RetryAttempts = 0,
            RetryIntervalMinutes = 0,
            AzureCredentialId = AzureCredentials.First().AzureCredentialId
        };

    protected override Task<DataflowStep> GetExistingStepAsync(AppDbContext context, Guid stepId) =>
        context.DataflowSteps
            .Include(step => step.Job)
            .Include(step => step.Tags)
            .Include(step => step.Dependencies)
            .Include(step => step.DataObjects)
            .ThenInclude(s => s.DataObject)
            .Include(step => step.ExecutionConditionParameters)
            .FirstAsync(step => step.StepId == stepId);

    protected override async Task OnModalShownAsync(DataflowStep step)
    {
        try
        {
            _loading = true;
            StateHasChanged();
            var azureCredential = AzureCredentials.First(a => a.AzureCredentialId == step.AzureCredentialId);
            var dataflowClient = azureCredential.CreateDataflowClient(tokenService, httpClientFactory);
            (step.WorkspaceName, step.DataflowName) =
                (await dataflowClient.GetWorkspaceNameAsync(step.WorkspaceId),
                    await dataflowClient.GetDataflowNameAsync(step.WorkspaceId, step.DataflowId));
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

    private Task OpenDataflowSelectOffcanvas()
    {
        ArgumentNullException.ThrowIfNull(Step);
        return _dataflowSelectOffcanvas.LetAsync(x => x.ShowAsync(Step.AzureCredentialId));   
    }

    private void OnDataflowSelected(Dataflow dataflow)
    {
        ArgumentNullException.ThrowIfNull(Step);
        (Step.WorkspaceId, Step.WorkspaceName, Step.DataflowId, Step.DataflowName) = dataflow;
    }
}