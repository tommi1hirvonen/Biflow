namespace Biflow.Ui.Shared.StepEditModal;

public partial class ScdStepEditModal(
    IMediator mediator,
    ToasterService toaster,
    IDbContextFactory<AppDbContext> dbContextFactory)
    : StepEditModal<ScdStep>(mediator, toaster, dbContextFactory)
{
    [Parameter] public IEnumerable<ScdTable> ScdTables { get; set; } = [];
    
    internal override string FormId => "scd_step_edit_form";
    
    protected override async Task<ScdStep> GetExistingStepAsync(AppDbContext context, Guid stepId)
    {
        var step = await context.ScdSteps
            .Include(step => step.Job)
            .Include(step => step.Tags)
            .Include(step => step.Dependencies)
            .Include(step => step.DataObjects)
            .ThenInclude(s => s.DataObject)
            .Include(step => step.ExecutionConditionParameters)
            .FirstAsync(step => step.StepId == stepId);
        // Data object editor pane of the step edit modal takes advantage of the step's connection.
        step.ConnectionId = ScdTables.FirstOrDefault(t => t.ScdTableId == step.ScdTableId)?.ConnectionId ?? Guid.Empty;
        return step;
    }
    
    protected override ScdStep CreateNewStep(Job job)
    {
        var table = ScdTables.FirstOrDefault();
        ArgumentNullException.ThrowIfNull(table);
        return new()
        {
            JobId = job.JobId,
            Job = job,
            RetryAttempts = 0,
            RetryIntervalMinutes = 0,
            TimeoutMinutes = 0,
            ScdTableId = Guid.Empty
        };
    }
    
    protected override async Task<ScdStep> OnSubmitCreateAsync(ScdStep step)
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
        var command = new CreateScdStepCommand
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
            ScdTableId = step.ScdTableId,
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

    protected override async Task<ScdStep> OnSubmitUpdateAsync(ScdStep step)
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
        var command = new UpdateScdStepCommand
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
            ScdTableId = step.ScdTableId,
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

    private void SetStepConnection()
    {
        ArgumentNullException.ThrowIfNull(Step);
        // Refresh the connection id.
        Step.ConnectionId = ScdTables
            .FirstOrDefault(t => t.ScdTableId == Step.ScdTableId)?.ConnectionId ?? Guid.Empty;
    }
    
    private Task<AutosuggestDataProviderResult<ScdTable>> GetSuggestionsAsync(AutosuggestDataProviderRequest request)
    {
        return Task.FromResult(new AutosuggestDataProviderResult<ScdTable>
        {
            Data = ScdTables
                .Where(t => t.ScdTableName.ContainsIgnoreCase(request.UserInput))
                .OrderBy(t => t.ScdTableName)
        });
    }
}