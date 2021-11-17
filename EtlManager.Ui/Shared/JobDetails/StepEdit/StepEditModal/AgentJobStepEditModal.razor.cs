using EtlManager.DataAccess;
using EtlManager.DataAccess.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;

namespace EtlManager.Ui.Shared.JobDetails.StepEdit.StepEditModal;

public partial class AgentJobStepEditModal : StepEditModalBase<AgentJobStep>
{
    [Parameter] public IList<SqlConnectionInfo>? Connections { get; set; }

    private AgentJobSelectOffcanvas AgentJobSelectOffcanvas { get; set; } = null!;

    internal override string FormId => "agent_job_step_edit_form";

    protected override AgentJobStep CreateNewStep(Job job)
    {
        return new(string.Empty)
        {
            JobId = job.JobId,
            RetryAttempts = 0,
            RetryIntervalMinutes = 0,
            IsEnabled = true,
            ConnectionId = Connections?.FirstOrDefault()?.ConnectionId,
            Dependencies = new List<Dependency>(),
            Tags = new List<Tag>()
        };
    }

    protected override async Task<AgentJobStep> GetExistingStepAsync(EtlManagerContext context, Guid stepId)
    {
        return await context.AgentJobSteps
                .Include(step => step.Tags)
                .Include(step => step.Dependencies)
                .FirstAsync(step => step.StepId == stepId);
    }

    private async Task OpenAgentJobSelectOffcanvas() => await AgentJobSelectOffcanvas.ShowAsync();

    private void OnAgentJobSelected(string agentJobName) => Step.AgentJobName = agentJobName;
}
