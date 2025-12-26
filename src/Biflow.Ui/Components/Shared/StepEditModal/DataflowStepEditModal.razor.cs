using Biflow.Ui.Components.Shared.StepEdit;

namespace Biflow.Ui.Components.Shared.StepEditModal;

public partial class DataflowStepEditModal(
    ITokenService tokenService,
    IHttpClientFactory httpClientFactory,
    IMediator mediator,
    ToasterService toaster,
    IDbContextFactory<AppDbContext> dbContextFactory)
    : StepEditModal<DataflowStep>(mediator, toaster, dbContextFactory)
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
            FabricWorkspaceId = Integrations.FabricWorkspaces.First().FabricWorkspaceId
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
    
    protected override async Task<DataflowStep> OnSubmitCreateAsync(DataflowStep step)
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
        var command = new CreateDataflowStepCommand
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
            TimeoutMinutes = step.TimeoutMinutes,
            FabricWorkspaceId = step.FabricWorkspaceId,
            DataflowId = Guid.Parse(step.DataflowId),
            DataflowName = step.DataflowName,
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

    protected override async Task<DataflowStep> OnSubmitUpdateAsync(DataflowStep step)
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
        var command = new UpdateDataflowStepCommand
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
            TimeoutMinutes = step.TimeoutMinutes,
            FabricWorkspaceId = step.FabricWorkspaceId,
            DataflowId = Guid.Parse(step.DataflowId),
            DataflowName = step.DataflowName,
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

    protected override async Task OnModalShownAsync(DataflowStep step)
    {
        if (string.IsNullOrEmpty(step.DataflowId))
        {
            return;
        }
        try
        {
            _loading = true;
            StateHasChanged();
            var fabricWorkspace = Integrations.FabricWorkspaces
                .First(w => w.FabricWorkspaceId == step.FabricWorkspaceId);
            var azureCredential = fabricWorkspace.AzureCredential;
            ArgumentNullException.ThrowIfNull(azureCredential);
            var dataflowClient = azureCredential.CreateDataflowClient(tokenService, httpClientFactory);
            step.DataflowName = await dataflowClient.GetDataflowNameAsync(fabricWorkspace.WorkspaceId, step.DataflowId);
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
        return _dataflowSelectOffcanvas.LetAsync(x => x.ShowAsync(Step.FabricWorkspaceId));   
    }

    private void OnDataflowSelected(Dataflow dataflow)
    {
        ArgumentNullException.ThrowIfNull(Step);
        var (_, dataflowId, dataflowName) = dataflow;
        (Step.DataflowId, Step.DataflowName) = (dataflowId.ToString(), dataflowName);
    }
}