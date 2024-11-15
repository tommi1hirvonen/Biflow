namespace Biflow.Ui.Shared.StepEditModal;

public partial class DbtStepEditModal(
    IHttpClientFactory httpClientFactory,
    ToasterService toaster,
    IDbContextFactory<AppDbContext> dbContextFactory)
    : StepEditModal<DbtStep>(toaster, dbContextFactory)
{
    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;

    [Parameter] public IEnumerable<DbtAccount> DbtAccounts { get; set; } = [];

    internal override string FormId => "dbt_step_edit_form";

    private DbtJob[]? dbtJobs;

    private DbtAccount? CurrentAccount =>
        DbtAccounts?.FirstOrDefault(a => a.DbtAccountId == Step?.DbtAccountId);

    protected override async Task<DbtStep> GetExistingStepAsync(AppDbContext context, Guid stepId)
    {
        var step = await context.DbtSteps
            .Include(step => step.Job)
            .Include(step => step.Tags)
            .Include(step => step.Dependencies)
            .Include(step => step.DataObjects)
            .ThenInclude(s => s.DataObject)
            .Include(step => step.ExecutionConditionParameters)
            .FirstAsync(step => step.StepId == stepId);
        return step;
    }

    protected override DbtStep CreateNewStep(Job job)
    {
        var client = DbtAccounts?.FirstOrDefault();
        ArgumentNullException.ThrowIfNull(client);
        return new()
        {
            JobId = job.JobId,
            Job = job,
            RetryAttempts = 0,
            RetryIntervalMinutes = 0,
            TimeoutMinutes = 0,
            DbtAccountId = client.DbtAccountId
        };
    }

    protected override Task OnSubmitAsync(AppDbContext context, DbtStep step)
    {
        step.DbtJobName ??= dbtJobs?.FirstOrDefault(j => j.Id == step.DbtJobId)?.Name;
        return Task.CompletedTask;
    }

    private async Task<DbtJob?> ResolveJobFromValueAsync(long value)
    {
        if (value <= 0)
        {
            return null;
        }
        if (dbtJobs is null)
        {
            try
            {
                var account = CurrentAccount;
                ArgumentNullException.ThrowIfNull(account);
                var client = account.CreateClient(_httpClientFactory);
                return await client.GetJobAsync(value);
            }
            catch (Exception ex)
            {
                Toaster.AddError("Error fetching dbt job", ex.Message);
                return null;
            }
        }
        return dbtJobs.FirstOrDefault(a => a.Id == value);
    }

    private async Task<AutosuggestDataProviderResult<DbtJob>> ProvideJobSuggestionsAsync(
        AutosuggestDataProviderRequest request)
    {
        if (dbtJobs is null)
        {
            try
            {
                var account = CurrentAccount;
                ArgumentNullException.ThrowIfNull(account);
                var client = account.CreateClient(_httpClientFactory);
                var dbtJobs = await client.GetJobsAsync();
                this.dbtJobs = dbtJobs.OrderBy(a => a.Name).ToArray();
            }
            catch (Exception ex)
            {
                Toaster.AddError("Error fetching dbt jobs", ex.Message);
                dbtJobs = [];
            }
        }

        return new()
        {
            Data = dbtJobs.Where(n => n.Name.ContainsIgnoreCase(request.UserInput))
        };
    }
}
