using Biflow.DataAccess;
using Biflow.DataAccess.Models;
using Biflow.Ui.Core;
using Biflow.Ui.Shared.StepEdit;
using Microsoft.EntityFrameworkCore;

namespace Biflow.Ui.Shared.StepEditModal;

public partial class AgentJobStepEditModal : StepEditModal<AgentJobStep>
{

    private AgentJobSelectOffcanvas? AgentJobSelectOffcanvas { get; set; }

    internal override string FormId => "agent_job_step_edit_form";

    protected override AgentJobStep CreateNewStep(Job job) =>
        new(string.Empty)
        {
            JobId = job.JobId,
            Job = job,
            RetryAttempts = 0,
            RetryIntervalMinutes = 0,
            ConnectionId = Connections?.FirstOrDefault()?.ConnectionId,
            Dependencies = new List<Dependency>(),
            Tags = new List<Tag>(),
            Sources = new List<DataObject>(),
            Targets = new List<DataObject>(),
            ExecutionConditionParameters = new List<ExecutionConditionParameter>()
        };

    protected override Task<AgentJobStep> GetExistingStepAsync(BiflowContext context, Guid stepId) =>
        context.AgentJobSteps
        .Include(step => step.Job)
        .Include(step => step.Tags)
        .Include(step => step.Dependencies)
        .Include(step => step.Sources)
        .Include(step => step.Targets)
        .Include(step => step.ExecutionConditionParameters)
        .FirstAsync(step => step.StepId == stepId);

    private Task OpenAgentJobSelectOffcanvas() => AgentJobSelectOffcanvas.LetAsync(x => x.ShowAsync());

    private void OnAgentJobSelected(string agentJobName)
    {
        ArgumentNullException.ThrowIfNull(Step);
        Step.AgentJobName = agentJobName;
    }
}
