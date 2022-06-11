using Biflow.DataAccess;
using Biflow.DataAccess.Models;
using Biflow.Ui.Shared.StepEdit;
using Microsoft.EntityFrameworkCore;

namespace Biflow.Ui.Shared.StepEditModal;

public partial class AgentJobStepEditModal : StepEditModalBase<AgentJobStep>
{

    private AgentJobSelectOffcanvas AgentJobSelectOffcanvas { get; set; } = null!;

    internal override string FormId => "agent_job_step_edit_form";

    protected override AgentJobStep CreateNewStep(Job job) =>
        new(string.Empty)
        {
            JobId = job.JobId,
            RetryAttempts = 0,
            RetryIntervalMinutes = 0,
            IsEnabled = true,
            ConnectionId = Connections?.FirstOrDefault()?.ConnectionId,
            Dependencies = new List<Dependency>(),
            Tags = new List<Tag>(),
            Sources = new List<SourceTargetObject>(),
            Targets = new List<SourceTargetObject>(),
            ExecutionConditionParameters = new List<ExecutionConditionParameter>()
        };

    protected override Task<AgentJobStep> GetExistingStepAsync(BiflowContext context, Guid stepId) =>
        context.AgentJobSteps
        .Include(step => step.Tags)
        .Include(step => step.Dependencies)
        .Include(step => step.Sources)
        .Include(step => step.Targets)
        .Include(step => step.ExecutionConditionParameters)
        .FirstAsync(step => step.StepId == stepId);

    private Task OpenAgentJobSelectOffcanvas() => AgentJobSelectOffcanvas.ShowAsync();

    private void OnAgentJobSelected(string agentJobName) => Step.AgentJobName = agentJobName;
}
