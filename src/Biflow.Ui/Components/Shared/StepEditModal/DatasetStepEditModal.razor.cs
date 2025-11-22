using Biflow.Ui.Components.Shared.StepEdit;

namespace Biflow.Ui.Components.Shared.StepEditModal;

public partial class DatasetStepEditModal(
    ITokenService tokenService,
    IMediator mediator,
    ToasterService toaster,
    IDbContextFactory<AppDbContext> dbContextFactory)
    : StepEditModal<DatasetStep>(mediator, toaster, dbContextFactory)
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
            AzureCredentialId = Integrations.AzureCredentials.First().AzureCredentialId
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
    
    protected override async Task<DatasetStep> OnSubmitCreateAsync(DatasetStep step)
    {
        var dependencies = step.Dependencies.ToDictionary(
            key => key.DependantOnStepId,
            value => value.DependencyType);
        var executionConditionParameters = step.ExecutionConditionParameters
            .Select(p => new CreateExecutionConditionParameter(
                p.ParameterName,
                p.ParameterValue,
                p.JobParameterId))
            .ToArray();
        var command = new CreateDatasetStepCommand
        {
            JobId = step.JobId,
            StepName = step.StepName ?? "",
            StepDescription = step.StepDescription,
            ExecutionPhase = step.ExecutionPhase,
            DuplicateExecutionBehaviour = step.DuplicateExecutionBehaviour,
            IsEnabled = step.IsEnabled,
            RetryAttempts = step.RetryAttempts,
            RetryIntervalMinutes = step.RetryIntervalMinutes,
            ExecutionConditionExpression = step.ExecutionConditionExpression.Expression,
            StepTagIds = step.Tags.Select(t => t.TagId).ToArray(),
            AzureCredentialId = step.AzureCredentialId,
            WorkspaceId = Guid.Parse(step.WorkspaceId),
            WorkspaceName = step.WorkspaceName,
            DatasetId = Guid.Parse(step.DatasetId),
            DatasetName = step.DatasetName,
            Dependencies = dependencies,
            ExecutionConditionParameters = executionConditionParameters,
            Sources = step.DataObjects
                .Where(x => x.ReferenceType == DataObjectReferenceType.Source)
                .Select(x => new DataObjectRelation(x.DataObject.ObjectId, x.DataAttributes.ToArray()))
                .ToArray(),
            Targets = step.DataObjects
                .Where(x => x.ReferenceType == DataObjectReferenceType.Target)
                .Select(x => new DataObjectRelation(x.DataObject.ObjectId, x.DataAttributes.ToArray()))
                .ToArray()
        };
        return await Mediator.SendAsync(command);
    }

    protected override async Task<DatasetStep> OnSubmitUpdateAsync(DatasetStep step)
    {
        var dependencies = step.Dependencies.ToDictionary(
            key => key.DependantOnStepId,
            value => value.DependencyType);
        var executionConditionParameters = step.ExecutionConditionParameters
            .Select(p => new UpdateExecutionConditionParameter(
                p.ParameterId,
                p.ParameterName,
                p.ParameterValue,
                p.JobParameterId))
            .ToArray();
        var command = new UpdateDatasetStepCommand
        {
            StepId = step.StepId,
            StepName = step.StepName ?? "",
            StepDescription = step.StepDescription,
            ExecutionPhase = step.ExecutionPhase,
            DuplicateExecutionBehaviour = step.DuplicateExecutionBehaviour,
            IsEnabled = step.IsEnabled,
            RetryAttempts = step.RetryAttempts,
            RetryIntervalMinutes = step.RetryIntervalMinutes,
            ExecutionConditionExpression = step.ExecutionConditionExpression.Expression,
            StepTagIds = step.Tags.Select(t => t.TagId).ToArray(),
            AzureCredentialId = step.AzureCredentialId,
            WorkspaceId = Guid.Parse(step.WorkspaceId),
            WorkspaceName = step.WorkspaceName,
            DatasetId = Guid.Parse(step.DatasetId),
            DatasetName = step.DatasetName,
            Dependencies = dependencies,
            ExecutionConditionParameters = executionConditionParameters,
            Sources = step.DataObjects
                .Where(x => x.ReferenceType == DataObjectReferenceType.Source)
                .Select(x => new DataObjectRelation(x.DataObject.ObjectId, x.DataAttributes.ToArray()))
                .ToArray(),
            Targets = step.DataObjects
                .Where(x => x.ReferenceType == DataObjectReferenceType.Target)
                .Select(x => new DataObjectRelation(x.DataObject.ObjectId, x.DataAttributes.ToArray()))
                .ToArray()
        };
        return await Mediator.SendAsync(command);
    }

    protected override async Task OnModalShownAsync(DatasetStep step)
    {
        if (string.IsNullOrEmpty(step.WorkspaceId) || string.IsNullOrEmpty(step.DatasetId))
        {
            return;
        }
        try
        {
            _loading = true;
            StateHasChanged();
            var azureCredential = Integrations.AzureCredentials.First(a => a.AzureCredentialId == step.AzureCredentialId);
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
