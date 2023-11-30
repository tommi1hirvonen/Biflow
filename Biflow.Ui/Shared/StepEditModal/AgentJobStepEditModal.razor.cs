using Biflow.DataAccess;
using Biflow.DataAccess.Models;
using Biflow.Ui.Core;
using Biflow.Ui.Shared.StepEdit;
using Microsoft.EntityFrameworkCore;

namespace Biflow.Ui.Shared.StepEditModal;

public partial class AgentJobStepEditModal : StepEditModal<AgentJobStep>
{

    private AgentJobSelectOffcanvas? agentJobSelectOffcanvas;

    internal override string FormId => "agent_job_step_edit_form";

    protected override AgentJobStep CreateNewStep(Job job) =>
        new(job.JobId, string.Empty)
        {
            Job = job,
            RetryAttempts = 0,
            RetryIntervalMinutes = 0,
            ConnectionId = Connections?.FirstOrDefault()?.ConnectionId,
            Dependencies = new List<Dependency>(),
            Tags = new List<Tag>(),
            Sources = new List<StepSource>(),
            Targets = new List<StepTarget>(),
            ExecutionConditionParameters = new List<ExecutionConditionParameter>()
        };

    protected override Task<AgentJobStep> GetExistingStepAsync(AppDbContext context, Guid stepId) =>
        context.AgentJobSteps
        .Include(step => step.Job)
        .Include(step => step.Tags)
        .Include(step => step.Dependencies)
        .Include(step => step.Sources)
        .ThenInclude(s => s.DataObject)
        .Include(step => step.Targets)
        .ThenInclude(t => t.DataObject)
        .Include(step => step.ExecutionConditionParameters)
        .FirstAsync(step => step.StepId == stepId);

    private Task OpenAgentJobSelectOffcanvas() => agentJobSelectOffcanvas.LetAsync(x => x.ShowAsync());

    private void OnAgentJobSelected(string agentJobName)
    {
        ArgumentNullException.ThrowIfNull(Step);
        Step.AgentJobName = agentJobName;
    }
}
