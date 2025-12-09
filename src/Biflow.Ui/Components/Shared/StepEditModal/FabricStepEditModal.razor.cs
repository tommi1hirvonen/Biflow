using Biflow.Ui.Components.Shared.StepEdit;
using Microsoft.Fabric.Api.DataPipeline.Models;
using Microsoft.Fabric.Api.Notebook.Models;

namespace Biflow.Ui.Components.Shared.StepEditModal;

public partial class FabricStepEditModal(
    ITokenService tokenService,
    IHttpClientFactory httpClientFactory,
    IMediator mediator,
    ToasterService toaster,
    IDbContextFactory<AppDbContext> dbContextFactory)
    : StepEditModal<FabricStep>(mediator, toaster, dbContextFactory)
{
    internal override string FormId => "fabric_step_edit_form";

    private FabricItemSelectOffcanvas? _offcanvas;
    private bool _loading;

    protected override FabricStep CreateNewStep(Job job) =>
        new()
        {
            JobId = job.JobId,
            Job = job,
            RetryAttempts = 0,
            RetryIntervalMinutes = 0,
            AzureCredentialId = Integrations.AzureCredentials.First().AzureCredentialId
        };

    protected override Task<FabricStep> GetExistingStepAsync(AppDbContext context, Guid stepId) =>
        context.FabricSteps
            .Include(step => step.Job)
            .ThenInclude(job => job.JobParameters)
            .Include(step => step.StepParameters)
            .ThenInclude(p => p.InheritFromJobParameter)
            .Include(step => step.StepParameters)
            .ThenInclude(p => p.ExpressionParameters)
            .Include(step => step.Tags)
            .Include(step => step.Dependencies)
            .Include(step => step.DataObjects)
            .ThenInclude(s => s.DataObject)
            .Include(step => step.ExecutionConditionParameters)
            .FirstAsync(step => step.StepId == stepId);
    
    protected override async Task<FabricStep> OnSubmitCreateAsync(FabricStep step)
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
        var parameters = step.StepParameters
            .Select(p => new CreateStepParameter(
                p.ParameterName,
                p.ParameterValue,
                p.UseExpression,
                p.Expression.Expression,
                p.InheritFromJobParameterId,
                p.ExpressionParameters
                    .Select(e => new CreateExpressionParameter(e.ParameterName, e.InheritFromJobParameterId))
                    .ToArray()))
            .ToArray();
        var command = new CreateFabricStepCommand
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
            AzureCredentialId = step.AzureCredentialId,
            WorkspaceId = step.WorkspaceId,
            WorkspaceName = step.WorkspaceName,
            ItemType = step.ItemType,
            ItemId = step.ItemId,
            ItemName = step.ItemName,
            Dependencies = dependencies,
            ExecutionConditionParameters = executionConditionParameters,
            Sources = step.DataObjects
                .Where(x => x.ReferenceType == DataObjectReferenceType.Source)
                .Select(x => new DataObjectRelation(x.DataObject.ObjectId, x.DataAttributes.ToArray()))
                .ToArray(),
            Targets = step.DataObjects
                .Where(x => x.ReferenceType == DataObjectReferenceType.Target)
                .Select(x => new DataObjectRelation(x.DataObject.ObjectId, x.DataAttributes.ToArray()))
                .ToArray(),
            Parameters = parameters
        };
        return await Mediator.SendAsync(command);
    }

    protected override async Task<FabricStep> OnSubmitUpdateAsync(FabricStep step)
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
        var parameters = step.StepParameters
            .Select(p => new UpdateStepParameter(
                p.ParameterId,
                p.ParameterName,
                p.ParameterValue,
                p.UseExpression,
                p.Expression.Expression,
                p.InheritFromJobParameterId,
                p.ExpressionParameters
                    .Select(e => new UpdateExpressionParameter(
                        e.ParameterId,
                        e.ParameterName,
                        e.InheritFromJobParameterId))
                    .ToArray()))
            .ToArray();
        var command = new UpdateFabricStepCommand
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
            AzureCredentialId = step.AzureCredentialId,
            WorkspaceId = step.WorkspaceId,
            WorkspaceName = step.WorkspaceName,
            ItemType = step.ItemType,
            ItemId = step.ItemId,
            ItemName = step.ItemName,
            Dependencies = dependencies,
            ExecutionConditionParameters = executionConditionParameters,
            Sources = step.DataObjects
                .Where(x => x.ReferenceType == DataObjectReferenceType.Source)
                .Select(x => new DataObjectRelation(x.DataObject.ObjectId, x.DataAttributes.ToArray()))
                .ToArray(),
            Targets = step.DataObjects
                .Where(x => x.ReferenceType == DataObjectReferenceType.Target)
                .Select(x => new DataObjectRelation(x.DataObject.ObjectId, x.DataAttributes.ToArray()))
                .ToArray(),
            Parameters = parameters
        };
        return await Mediator.SendAsync(command);
    }
    
    protected override async Task OnModalShownAsync(FabricStep step)
    {
        if (step.WorkspaceId == Guid.Empty || step.ItemId == Guid.Empty)
        {
            return;
        }
        try
        {
            _loading = true;
            StateHasChanged();
            var azureCredential = Integrations.AzureCredentials
                .First(a => a.AzureCredentialId == step.AzureCredentialId);
            var fabric = azureCredential.CreateFabricWorkspaceClient(tokenService, httpClientFactory);
            (step.WorkspaceName, step.ItemName) = 
                (await fabric.GetWorkspaceNameAsync(step.WorkspaceId), 
                    await fabric.GetItemNameAsync(step.WorkspaceId, step.ItemId));
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
    
    private Task OpenItemSelectOffcanvas()
    {
        ArgumentNullException.ThrowIfNull(Step);
        return _offcanvas.LetAsync(x => x.ShowAsync(Step.AzureCredentialId));   
    }

    private void OnItemSelected(FabricItemSelectedResult result)
    {
        ArgumentNullException.ThrowIfNull(Step);
        var (workspaceId, workspaceName, item) = result;
        if (!item.Id.HasValue)
        {
            throw new ArgumentNullException(nameof(item.Id));
        }
        Step.WorkspaceId = workspaceId;
        Step.WorkspaceName = workspaceName;
        switch (item)
        {
            case DataPipeline:
                Step.ItemId = (Guid)item.Id;
                Step.ItemName = item.DisplayName;
                Step.ItemType = FabricItemType.DataPipeline;
                break;
            case Notebook:
                Step.ItemId = (Guid)item.Id;
                Step.ItemName = item.DisplayName;
                Step.ItemType = FabricItemType.Notebook;
                break;
        }
    }
}