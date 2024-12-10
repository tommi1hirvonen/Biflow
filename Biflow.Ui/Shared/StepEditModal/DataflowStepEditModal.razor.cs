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
    private string? _dataflowGroupName;
    private string? _dataflowName;

    private void OnDataflowSelected(Dataflow dataflow)
    {
        ArgumentNullException.ThrowIfNull(Step);
        (Step.WorkspaceId, _dataflowGroupName, Step.DataflowId, _dataflowName) = dataflow;
    }

    private Task OpenDataflowSelectOffcanvas()
    {
        ArgumentNullException.ThrowIfNull(Step);
        return _dataflowSelectOffcanvas.LetAsync(x => x.ShowAsync(Step.AzureCredentialId));   
    }

    protected override async Task OnModalShownAsync(DataflowStep step)
    {
        try
        {
            var azureCredential = AzureCredentials.First(a => a.AzureCredentialId == step.AzureCredentialId);
            var dataflowClient = azureCredential.CreateDataflowClient(tokenService, httpClientFactory);
            (_dataflowGroupName, _dataflowName) = azureCredential switch
            {
                not null => (
                    await dataflowClient.GetWorkspaceNameAsync(step.WorkspaceId),
                    await dataflowClient.GetDataflowNameAsync(step.WorkspaceId, step.DataflowId)
                    ),
                _ => ("", "")
            };
        }
        catch
        {
            (_dataflowGroupName, _dataflowName) = ("", "");
        }
        finally
        {
            StateHasChanged();
        }
    }

    protected override async Task<DataflowStep> GetExistingStepAsync(AppDbContext context, Guid stepId)
    {
        (_dataflowGroupName, _dataflowName) = (null, null);
        var step = await context.DataflowSteps
            .Include(step => step.Job)
            .Include(step => step.Tags)
            .Include(step => step.Dependencies)
            .Include(step => step.DataObjects)
            .ThenInclude(s => s.DataObject)
            .Include(step => step.ExecutionConditionParameters)
            .FirstAsync(step => step.StepId == stepId);
        
        return step;
    }

    protected override DataflowStep CreateNewStep(Job job)
    {
        (_dataflowGroupName, _dataflowName) = ("", "");
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