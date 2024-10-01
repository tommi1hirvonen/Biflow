using Biflow.Ui.Shared.StepEdit;
using System.Globalization;

namespace Biflow.Ui.Shared.StepEditModal;

public partial class DbNotebookStepEditModal : StepEditModal<DbNotebookStep>
{
    internal override string FormId => "dbnotebook_step_edit_form";

    private DbNotebookSelectOffcanvas? notebookSelectOffcanvas;

    private (string Id, string Description)[]? runtimeVersions;

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

    private void OnWorkspaceChanged()
    {
        runtimeVersions = null;
    }

    private async Task<(string Id, string Description)> ResolveRuntimeVersionFromValueAsync(string? value)
    {
        if (runtimeVersions is null)
        {
            try
            {
                var workspace = DatabricksWorkspaces?.FirstOrDefault();
                ArgumentNullException.ThrowIfNull(workspace);
                using var client = workspace.CreateClient();
                var runtimeVersions = await client.GetRuntimeVersionsAsync();
                this.runtimeVersions = runtimeVersions.Select(v => (v.Key, v.Value)).ToArray();
            }
            catch (Exception ex)
            {
                Toaster.AddError("Error fetching available runtimes", ex.Message);
                runtimeVersions = [];
            }
        }
        return runtimeVersions.FirstOrDefault(v => v.Id == value);
    }

    private async Task<AutosuggestDataProviderResult<(string Id, string Description)>> ProvideRuntimeVersionSuggestionsAsync(
        AutosuggestDataProviderRequest request)
    {
        if (runtimeVersions is null)
        {
            try
            {
                var workspace = DatabricksWorkspaces?.FirstOrDefault();
                ArgumentNullException.ThrowIfNull(workspace);
                using var client = workspace.CreateClient();
                var runtimeVersions = await client.GetRuntimeVersionsAsync();
                this.runtimeVersions = runtimeVersions.Select(v => (v.Key, v.Value)).ToArray();
            }
            catch (Exception ex)
            {
                Toaster.AddError("Error fetching available runtimes", ex.Message);
                runtimeVersions = [];
            }
        }
        
        return new()
        {
            Data = runtimeVersions
                .Where(v => v.Description.ContainsIgnoreCase(request.UserInput))
                .OrderByDescending(v =>
                {
                    var span = v.Description.AsSpan();
                    var index = span.IndexOf(' ');
                    var version = index switch
                    {
                        > 0 when double.TryParse(span[0..index], NumberFormatInfo.InvariantInfo, out var num) => num,
                        _ => 0
                    };
                    return version;
                })
        };
    }
}
