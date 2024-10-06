using Biflow.Ui.Shared.StepEdit;
using System.Globalization;
using Pipeline = Microsoft.Azure.Databricks.Client.Models.Pipeline;

namespace Biflow.Ui.Shared.StepEditModal;

public partial class DatabricksStepEditModal : StepEditModal<DatabricksStep>
{
    internal override string FormId => "databricks_step_edit_form";

    private DatabricksFileSelectOffcanvas? fileSelectOffcanvas;

    private (string Id, string Description)[]? runtimeVersions;
    private (string Id, string Description)[]? nodeTypes;
    private (string Id, string Description)[]? clusters;
    private DatabricksJob[]? dbJobs;
    private Pipeline[]? pipelines;

    private string ParametersTitle => Step?.DatabricksStepSettings switch
    {
        DbNotebookStepSettings => "Notebook parameters",
        DbPythonFileStepSettings => "Command line arguments",
        DbJobStepSettings => "Job run parameters",
        _ => ""
    };

    private string ParametersInfoContent => Step?.DatabricksStepSettings switch
    {
        DbNotebookStepSettings => "<div><p>Use parameters to dynamically pass values to the notebook.</p><p>The parameters are passed as key-value pairs.</p></div>",
        DbPythonFileStepSettings => "<div><p>Use parameters to dynamically pass command line arguments to the Python file task.</p><p>The parameters are ordered by their name and only the values are passed as arguments. Use the parameter name field to control the order in which values are provided as arguments.</p></div>",
        DbJobStepSettings => "<div><p>Use parameters to dynamically pass values to the job run.</p><p>The parameters are passed as key-value pairs.</p></div>",
        _ => ""
    };

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
        // Store the pipeline or job name only for audit purposes.
        if (step.DatabricksStepSettings is DbPipelineStepSettings pipeline)
        {
            pipeline.PipelineName ??= pipelines
                ?.FirstOrDefault(p => p.PipelineId == pipeline.PipelineId)
                ?.Name;
        }
        else if (step.DatabricksStepSettings is DbJobStepSettings job)
        {
            job.JobName ??= dbJobs
                ?.FirstOrDefault(j => j.JobId == job.JobId)
                ?.JobName;
        }

        // Also store the cluster name if applicable.
        if (step.DatabricksStepSettings is DatabricksClusterStepSettings { ClusterConfiguration: ExistingClusterConfiguration existing })
        {
            existing.ClusterName ??= clusters
                ?.FirstOrDefault(c => c.Id == existing.ClusterId)
                .Description;
        }

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
        pipelines = null;
    }

    private async Task<Pipeline?> ResolvePipelineFromValueAsync(string value)
    {
        if (pipelines is null)
        {
            try
            {
                var workspace = DatabricksWorkspaces?.FirstOrDefault();
                ArgumentNullException.ThrowIfNull(workspace);
                using var client = workspace.CreateClient();
                var pipelines = await client.GetPipelinesAsync();
                this.pipelines = pipelines.ToArray();
            }
            catch (Exception ex)
            {
                Toaster.AddError("Error fetching Databricks pipelines", ex.Message);
                pipelines = [];
            }
        }
        return pipelines.FirstOrDefault(v => v.PipelineId == value);
    }

    private async Task<AutosuggestDataProviderResult<Pipeline>> ProvidePipelineSuggestionsAsync(
        AutosuggestDataProviderRequest request)
    {
        if (pipelines is null)
        {
            try
            {
                var workspace = DatabricksWorkspaces?.FirstOrDefault();
                ArgumentNullException.ThrowIfNull(workspace);
                using var client = workspace.CreateClient();
                var pipelines = await client.GetPipelinesAsync();
                this.pipelines = pipelines.ToArray();
            }
            catch (Exception ex)
            {
                Toaster.AddError("Error fetching Databricks pipelines", ex.Message);
                pipelines = [];
            }
        }

        return new()
        {
            Data = pipelines
                .Where(n => n.Name.ContainsIgnoreCase(request.UserInput))
        };
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
            .Select(c => (c.ClusterId, c.ClusterName))
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
