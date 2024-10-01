using Biflow.Ui.Shared.StepEdit;

namespace Biflow.Ui.Shared.StepEditModal;

public partial class DbNotebookStepEditModal : StepEditModal<DbNotebookStep>
{
    internal override string FormId => "dbnotebook_step_edit_form";

    private DbNotebookSelectOffcanvas? notebookSelectOffcanvas;

    protected override async Task<DbNotebookStep> GetExistingStepAsync(AppDbContext context, Guid stepId)
    {
        var step = await context.DbNotebookSteps
            .Include(step => step.Job)
            .Include(step => step.Tags)
            .Include(step => step.Dependencies)
            .Include(step => step.DataObjects)
            .ThenInclude(s => s.DataObject)
            .Include(step => step.ExecutionConditionParameters)
            .FirstAsync(step => step.StepId == stepId);
        return step;
    }

    protected override DbNotebookStep CreateNewStep(Job job)
    {
        var workspace = DatabricksWorkspaces?.FirstOrDefault();
        ArgumentNullException.ThrowIfNull(workspace);
        return new()
        {
            JobId = job.JobId,
            Job = job,
            RetryAttempts = 0,
            RetryIntervalMinutes = 0,
            TimeoutMinutes = 0,
            DatabricksWorkspaceId = workspace.WorkspaceId
        };
    }

    protected override Task OnSubmitAsync(AppDbContext context, DbNotebookStep step)
    {
        // Change tracking does not identify changes to cluster configuration.
        // Tell the change tracker that the config has changed just in case.
        context.Entry(step).Property(p => p.ClusterConfiguration).IsModified = true;
        return Task.CompletedTask;
    }

    private Task OpenNotebookSelectOffcanvas()
    {
        ArgumentNullException.ThrowIfNull(Step?.DatabricksWorkspaceId);
        return notebookSelectOffcanvas.LetAsync(x => x.ShowAsync(Step.DatabricksWorkspaceId));
    }

    private void OnNotebookSelected(string notebook)
    {
        ArgumentNullException.ThrowIfNull(Step);
        Step.NotebookPath = notebook;
    }
}
