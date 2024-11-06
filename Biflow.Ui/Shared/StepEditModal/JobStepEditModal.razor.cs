namespace Biflow.Ui.Shared.StepEditModal;

public partial class JobStepEditModal(
    ToasterService toaster, IDbContextFactory<AppDbContext> dbContextFactory)
    : StepEditModal<JobStep>(toaster, dbContextFactory)
{
    internal override string FormId => "job_step_edit_form";

    protected override JobStep CreateNewStep(Job job) =>
        new()
        {
            JobId = job.JobId,
            Job = job,
            RetryAttempts = 0,
            RetryIntervalMinutes = 0,
            JobToExecuteId = null
        };

    protected override async Task<JobStep> GetExistingStepAsync(AppDbContext context, Guid stepId)
    {
        var step = await context.JobSteps
            .Include(step => step.Job)
            .ThenInclude(job => job.JobParameters)
            .Include(step => step.Tags)
            .Include(step => step.TagFilters)
            .Include(step => step.Dependencies)
            .Include(step => step.DataObjects)
            .ThenInclude(s => s.DataObject)
            .Include(step => step.StepParameters)
            .ThenInclude(p => p.InheritFromJobParameter)
            .Include(step => step.StepParameters)
            .ThenInclude(p => p.ExpressionParameters)
            .Include(step => step.ExecutionConditionParameters)
            .FirstAsync(step => step.StepId == stepId);
        SetJobToExecute();
        return step;
    }

    private Task<AutosuggestDataProviderResult<JobProjection>> GetSuggestionsAsync(AutosuggestDataProviderRequest request)
    {
        return Task.FromResult(new AutosuggestDataProviderResult<JobProjection>
        {
            Data = JobSlims?.Values
                .Where(j => j.JobId != Step?.JobId)
                .Where(j => j.JobName.ContainsIgnoreCase(request.UserInput))
                .OrderBy(j => j.JobName)
                .AsEnumerable()
                ?? []
        });
    }

    private void SetJobToExecute()
    {
        Step?.StepParameters.Clear();
    }

}
