using Biflow.Ui.Shared.StepEdit;

namespace Biflow.Ui.Shared.StepEditModal;

public partial class DbtStepEditModal(
    ToasterService toaster,
    IDbContextFactory<AppDbContext> dbContextFactory)
    : StepEditModal<DbtStep>(toaster, dbContextFactory)
{
    [Parameter] public IEnumerable<DbtAccount> DbtAccounts { get; set; } = [];

    internal override string FormId => "dbt_step_edit_form";

    private DbtJobSelectOffcanvas? _jobSelectOffcanvas;

    private DbtAccount? CurrentAccount =>
        DbtAccounts.FirstOrDefault(a => a.DbtAccountId == Step?.DbtAccountId);

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
        var client = DbtAccounts.FirstOrDefault();
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

    private Task OpenJobSelectOffcanvas()
    {
        var account = CurrentAccount;
        ArgumentNullException.ThrowIfNull(account);
        return _jobSelectOffcanvas.LetAsync(x => x.ShowAsync(account));
    }

    private void OnJobSelected((DbtProject, DbtEnvironment, DbtJob) selectedJob)
    {
        ArgumentNullException.ThrowIfNull(Step);
        var (project, environment, job) = selectedJob;
        Step.DbtJob = new()
        {
            Id = job.Id,
            Name = job.Name,
            EnvironmentId = environment.Id,
            EnvironmentName = environment.Name,
            ProjectId = project.Id,
            ProjectName = project.Name
        };
    }
}
