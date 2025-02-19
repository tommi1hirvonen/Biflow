using Biflow.Ui.Shared.StepEdit;

namespace Biflow.Ui.Shared.StepEditModal;

public partial class DbtStepEditModal(
    IMediator mediator,
    ToasterService toaster,
    IDbContextFactory<AppDbContext> dbContextFactory)
    : StepEditModal<DbtStep>(mediator, toaster, dbContextFactory)
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
    
    protected override async Task<DbtStep> OnSubmitCreateAsync(DbtStep step)
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
        var command = new CreateDbtStepCommand
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
            DbtAccountId = step.DbtAccountId,
            DbtJob = step.DbtJob,
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

    protected override async Task<DbtStep> OnSubmitUpdateAsync(DbtStep step)
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
        var command = new UpdateDbtStepCommand
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
            DbtAccountId = step.DbtAccountId,
            DbtJob = step.DbtJob,
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
