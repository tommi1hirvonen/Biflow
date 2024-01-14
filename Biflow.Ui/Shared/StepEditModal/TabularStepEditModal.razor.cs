using Biflow.Ui.Shared.StepEdit;

namespace Biflow.Ui.Shared.StepEditModal;

public partial class TabularStepEditModal : StepEditModal<TabularStep>
{
    internal override string FormId => "tabular_step_edit_form";

    private AnalysisServicesObjectSelectOffcanvas? offcanvas;

    protected override TabularStep CreateNewStep(Job job) =>
        new()
        {
            JobId = job.JobId,
            Job = job,
            RetryAttempts = 0,
            RetryIntervalMinutes = 0,
            ConnectionId = AsConnections.First().ConnectionId
        };

    protected override Task<TabularStep> GetExistingStepAsync(AppDbContext context, Guid stepId) =>
        context.TabularSteps
        .Include(step => step.Job)
        .Include(step => step.Tags)
        .Include(step => step.Dependencies)
        .Include(step => step.DataObjects)
        .ThenInclude(s => s.DataObject)
        .Include(step => step.ExecutionConditionParameters)
        .FirstAsync(step => step.StepId == stepId);

    private void OnAnalysisServicesObjectSelected(AnalysisServicesObjectSelectedResponse obj)
    {
        ArgumentNullException.ThrowIfNull(Step);
        (Step.TabularModelName, Step.TabularTableName, Step.TabularPartitionName) = obj;
    }

}
