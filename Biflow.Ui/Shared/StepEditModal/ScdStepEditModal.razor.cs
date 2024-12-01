namespace Biflow.Ui.Shared.StepEditModal;

public partial class ScdStepEditModal(
    ToasterService toaster,
    IDbContextFactory<AppDbContext> dbContextFactory) : StepEditModal<ScdStep>(toaster, dbContextFactory)
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

    private void SetStepConnection()
    {
        ArgumentNullException.ThrowIfNull(Step);
        // Refresh the connection id.
        Step.ConnectionId = ScdTables
            .FirstOrDefault(t => t.ScdTableId == Step.ScdTableId)?.ConnectionId ?? Guid.Empty;
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
            ScdTableId = null
        };
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