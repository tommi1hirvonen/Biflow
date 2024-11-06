using Biflow.Ui.Shared.StepEdit;

namespace Biflow.Ui.Shared.StepEditModal;

public partial class AgentJobStepEditModal(
    ToasterService toaster, IDbContextFactory<AppDbContext> dbContextFactory)
    : StepEditModal<AgentJobStep>(toaster, dbContextFactory)
{
    private AgentJobSelectOffcanvas? agentJobSelectOffcanvas;

    internal override string FormId => "agent_job_step_edit_form";

    private MsSqlConnection Connection
    {
        get
        {
            if (_connection is null || _connection.ConnectionId != Step?.ConnectionId)
            {
                _connection = MsSqlConnections
                    .FirstOrDefault(c => c.ConnectionId == Step?.ConnectionId)
                    ?? MsSqlConnections.First();
            }
            return _connection;
        }
    }

    private MsSqlConnection? _connection = null;

    protected override AgentJobStep CreateNewStep(Job job) =>
        new()
        {
            JobId = job.JobId,
            Job = job,
            RetryAttempts = 0,
            RetryIntervalMinutes = 0,
            ConnectionId = MsSqlConnections.First().ConnectionId
        };

    protected override Task<AgentJobStep> GetExistingStepAsync(AppDbContext context, Guid stepId) =>
        context.AgentJobSteps
        .Include(step => step.Job)
        .Include(step => step.Tags)
        .Include(step => step.Dependencies)
        .Include(step => step.DataObjects)
        .ThenInclude(s => s.DataObject)
        .Include(step => step.ExecutionConditionParameters)
        .FirstAsync(step => step.StepId == stepId);

    private Task OpenAgentJobSelectOffcanvas() => agentJobSelectOffcanvas.LetAsync(x => x.ShowAsync());

    private void OnAgentJobSelected(string agentJobName)
    {
        ArgumentNullException.ThrowIfNull(Step);
        Step.AgentJobName = agentJobName;
    }
}
