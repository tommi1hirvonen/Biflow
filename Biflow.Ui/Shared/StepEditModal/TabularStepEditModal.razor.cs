using Biflow.DataAccess;
using Biflow.DataAccess.Models;
using Biflow.Ui.Shared.StepEdit;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;

namespace Biflow.Ui.Shared.StepEditModal;

public partial class TabularStepEditModal : StepEditModal<TabularStep>
{
    [Parameter] public IList<AnalysisServicesConnectionInfo>? AsConnections { get; set; }

    private AnalysisServicesObjectSelectOffcanvas? Offcanvas { get; set; }

    internal override string FormId => "tabular_step_edit_form";

    protected override TabularStep CreateNewStep(Job job) =>
        new(string.Empty)
        {
            JobId = job.JobId,
            Job = job,
            RetryAttempts = 0,
            RetryIntervalMinutes = 0,
            IsEnabled = true,
            ConnectionId = AsConnections?.FirstOrDefault()?.ConnectionId,
            Dependencies = new List<Dependency>(),
            Tags = new List<Tag>(),
            Sources = new List<DataObject>(),
            Targets = new List<DataObject>(),
            ExecutionConditionParameters = new List<ExecutionConditionParameter>()
        };

    protected override Task<TabularStep> GetExistingStepAsync(BiflowContext context, Guid stepId) =>
        context.TabularSteps
        .Include(step => step.Job)
        .Include(step => step.Tags)
        .Include(step => step.Dependencies)
        .Include(step => step.Sources)
        .Include(step => step.Targets)
        .Include(step => step.ExecutionConditionParameters)
        .FirstAsync(step => step.StepId == stepId);

    private void OnAnalysisServicesObjectSelected(AnalysisServicesObjectSelectedResponse obj)
    {
        ArgumentNullException.ThrowIfNull(Step);
        (Step.TabularModelName, Step.TabularTableName, Step.TabularPartitionName) = obj;
    }

}
