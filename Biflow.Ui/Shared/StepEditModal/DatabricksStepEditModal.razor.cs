using Biflow.Ui.Shared.StepEdit;
using System.Globalization;

namespace Biflow.Ui.Shared.StepEditModal;

public partial class DatabricksStepEditModal : StepEditModal<DatabricksStep>
{
    internal override string FormId => "databricks_step_edit_form";

    private DatabricksFileSelectOffcanvas? fileSelectOffcanvas;

    private (string Id, string Description)[]? runtimeVersions;
    private (string Id, string Description)[]? nodeTypes;
    private (string Id, string Description)[]? clusters;
    private DatabricksJob[]? dbJobs;

    protected override async Task<DatabricksStep> GetExistingStepAsync(AppDbContext context, Guid stepId)
    {
        var step = await context.DatabricksSteps
            .Include(step => step.Job).ThenInclude(job => job.JobParameters)
            .Include(step => step.StepParameters).ThenInclude(p => p.InheritFromJobParameter)
            .Include(step => step.StepParameters).ThenInclude(p => p.ExpressionParameters)
            .Include(step => step.Tags)
            .Include(step => step.Dependencies)
            .Include(step => step.DataObjects).ThenInclude(s => s.DataObject)
            .Include(step => step.ExecutionConditionParameters)
            .FirstAsync(step => step.StepId == stepId);
        return step;
    }

    protected override DatabricksStep CreateNewStep(Job job)
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

    protected override Task OnSubmitAsync(AppDbContext context, DatabricksStep step)
    {
        // Change tracking does not identify changes to cluster configuration.
        // Tell the change tracker that the config has changed just in case.
        context.Entry(step).Property(p => p.DatabricksStepSettings).IsModified = true;
        return Task.CompletedTask;
    }

    private Task OpenFileSelectOffcanvas()
    {
        ArgumentNullException.ThrowIfNull(Step?.DatabricksWorkspaceId);
        return fileSelectOffcanvas.LetAsync(x => x.ShowAsync(Step.DatabricksWorkspaceId));
    }

    private void OnFileSelected(string filePath)
    {
        ArgumentNullException.ThrowIfNull(Step);
        if (Step.DatabricksStepSettings is DbNotebookStepSettings notebookSettings)
        {
            notebookSettings.NotebookPath = filePath;
        }
        else if (Step.DatabricksStepSettings is DbPythonFileStepSettings pythonSettings)
        {
            pythonSettings.FilePath = filePath;
        }
    }

    private void OnWorkspaceChanged()
    {
        runtimeVersions = null;
        nodeTypes = null;
        clusters = null;
        dbJobs = null;
    }

    private async Task<DatabricksJob?> ResolveDbJobFromValueAsync(long value)
    {
        if (dbJobs is null)
        {
            try
            {
                var workspace = DatabricksWorkspaces?.FirstOrDefault();
                ArgumentNullException.ThrowIfNull(workspace);
                using var client = workspace.CreateClient();
                dbJobs = (await client.GetJobsAsync()).ToArray();
            }
            catch (Exception ex)
            {
                Toaster.AddError("Error fetching Databricks jobs", ex.Message);
                dbJobs = [];
            }
        }
        return dbJobs.FirstOrDefault(v => v.JobId == value);
    }

    private async Task<AutosuggestDataProviderResult<DatabricksJob>> ProvideDbJobSuggestionsAsync(
        AutosuggestDataProviderRequest request)
    {
        if (dbJobs is null)
        {
            try
            {
                var workspace = DatabricksWorkspaces?.FirstOrDefault();
                ArgumentNullException.ThrowIfNull(workspace);
                using var client = workspace.CreateClient();
                dbJobs = (await client.GetJobsAsync()).ToArray();
            }
            catch (Exception ex)
            {
                Toaster.AddError("Error fetching Databricks jobs", ex.Message);
                dbJobs = [];
            }
        }

        return new()
        {
            Data = dbJobs
                .Where(n => n.JobName.ContainsIgnoreCase(request.UserInput))
        };
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
                    // Runtime version descriptions are in the format "15.4 LTS (includes Apache Spark 3.5.0, Scala 2.12)".
                    // Get the index of the first space and try to extract the version number as double.
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

    private async Task<(string Id, string Description)[]> GetNodeTypesAsync()
    {
        var workspace = DatabricksWorkspaces?.FirstOrDefault();
        ArgumentNullException.ThrowIfNull(workspace);
        using var client = workspace.CreateClient();
        var nodeTypes = await client.GetNodeTypesAsync();
        return nodeTypes
            .Select(n =>
            {
                var memory = Convert.ToInt32(n.MemoryMb / 1024);
                var cores = Convert.ToInt32(n.NumCores);
                return (n.NodeTypeId, $"{n.NodeTypeId} ({memory} GB, {cores} cores)");
            })
            .ToArray();
    }

    private async Task<(string Id, string Description)> ResolveNodeTypeFromValueAsync(string? value)
    {
        if (nodeTypes is null)
        {
            try
            {
                nodeTypes = await GetNodeTypesAsync();
            }
            catch (Exception ex)
            {
                Toaster.AddError("Error fetching available node types", ex.Message);
                nodeTypes = [];
            }
        }
        return nodeTypes.FirstOrDefault(v => v.Id == value);
    }

    private async Task<AutosuggestDataProviderResult<(string Id, string Description)>> ProvideNodeTypeSuggestionsAsync(
        AutosuggestDataProviderRequest request)
    {
        if (nodeTypes is null)
        {
            try
            {
                nodeTypes = await GetNodeTypesAsync();
            }
            catch (Exception ex)
            {
                Toaster.AddError("Error fetching available node types", ex.Message);
                nodeTypes = [];
            }
        }

        return new()
        {
            Data = nodeTypes
                .Where(n => n.Description.ContainsIgnoreCase(request.UserInput))
        };
    }

    private async Task<(string Id, string Description)[]> GetClustersAsync()
    {
        var workspace = DatabricksWorkspaces?.FirstOrDefault();
        ArgumentNullException.ThrowIfNull(workspace);
        using var client = workspace.CreateClient();
        var clusters = await client.GetClustersAsync();
        return clusters
            .Where(c => c.ClusterSource == Microsoft.Azure.Databricks.Client.Models.ClusterSource.UI)
            .Select(c =>
            {
                var memory = Convert.ToInt32(c.ClusterMemoryMb/ 1024);
                var cores = Convert.ToInt32(c.ClusterCores);
                return (c.ClusterId, $"{c.ClusterName} ({memory} GB, {cores} cores)");
            })
            .ToArray();
    }

    private async Task<(string Id, string Description)> ResolveClusterFromValueAsync(string? value)
    {
        if (clusters is null)
        {
            try
            {
                clusters = await GetClustersAsync();
            }
            catch (Exception ex)
            {
                Toaster.AddError("Error fetching available clusters", ex.Message);
                clusters = [];
            }
        }
        return clusters.FirstOrDefault(v => v.Id == value);
    }

    private async Task<AutosuggestDataProviderResult<(string Id, string Description)>> ProvideClusterSuggestionsAsync(
        AutosuggestDataProviderRequest request)
    {
        if (clusters is null)
        {
            try
            {
                clusters = await GetClustersAsync();
            }
            catch (Exception ex)
            {
                Toaster.AddError("Error fetching available clusters", ex.Message);
                clusters = [];
            }
        }

        return new()
        {
            Data = clusters
                .Where(n => n.Description.ContainsIgnoreCase(request.UserInput))
        };
    }
}
