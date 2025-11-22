using System.Globalization;
using Biflow.Ui.Components.Shared.StepEdit;
using Pipeline = Microsoft.Azure.Databricks.Client.Models.Pipeline;
using ClusterInfo = Microsoft.Azure.Databricks.Client.Models.ClusterInfo;

namespace Biflow.Ui.Components.Shared.StepEditModal;

public partial class DatabricksStepEditModal(
    IMediator mediator,
    ToasterService toaster,
    IDbContextFactory<AppDbContext> dbContextFactory)
    : StepEditModal<DatabricksStep>(mediator, toaster, dbContextFactory)
{
    internal override string FormId => "databricks_step_edit_form";

    private DatabricksFileSelectOffcanvas? _fileSelectOffcanvas;

    private (string Id, string Description)[]? _runtimeVersions;
    private (string Id, string Description)[]? _nodeTypes;
    private ClusterInfo[]? _clusters;
    private DatabricksJob[]? _dbJobs;
    private Pipeline[]? _pipelines;

    private DatabricksWorkspace? CurrentWorkspace =>
        Integrations.DatabricksWorkspaces.FirstOrDefault(w => w.WorkspaceId == Step?.DatabricksWorkspaceId);

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
        var workspace = Integrations.DatabricksWorkspaces.FirstOrDefault();
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
    
    protected override async Task<DatabricksStep> OnSubmitCreateAsync(DatabricksStep step)
    {
        switch (step.DatabricksStepSettings)
        {
            // Store the pipeline or job name only for audit purposes.
            case DbPipelineStepSettings pipeline:
                pipeline.PipelineName ??= _pipelines
                    ?.FirstOrDefault(p => p.PipelineId == pipeline.PipelineId)
                    ?.Name;
                break;
            case DbJobStepSettings job:
                job.JobName ??= _dbJobs
                    ?.FirstOrDefault(j => j.JobId == job.JobId)
                    ?.JobName;
                break;
            // Also store the cluster name if applicable.
            case DatabricksClusterStepSettings { ClusterConfiguration: ExistingClusterConfiguration existing }:
                existing.ClusterName ??= _clusters
                    ?.FirstOrDefault(c => c.ClusterId == existing.ClusterId)
                    ?.ClusterName;
                break;
        }
        var dependencies = step.Dependencies.ToDictionary(
            key => key.DependantOnStepId,
            value => value.DependencyType);
        var executionConditionParameters = step.ExecutionConditionParameters
            .Select(p => new CreateExecutionConditionParameter(
                p.ParameterName,
                p.ParameterValue,
                p.JobParameterId))
            .ToArray();
        var parameters = step.StepParameters
            .Select(p => new CreateStepParameter(
                p.ParameterName,
                p.ParameterValue,
                p.UseExpression,
                p.Expression.Expression,
                p.InheritFromJobParameterId,
                p.ExpressionParameters
                    .Select(e => new CreateExpressionParameter(e.ParameterName, e.InheritFromJobParameterId))
                    .ToArray()))
            .ToArray();
        var command = new CreateDatabricksStepCommand
        {
            JobId = step.JobId,
            StepName = step.StepName ?? "",
            StepDescription = step.StepDescription,
            ExecutionPhase = step.ExecutionPhase,
            DuplicateExecutionBehaviour = step.DuplicateExecutionBehaviour,
            IsEnabled = step.IsEnabled,
            RetryAttempts = step.RetryAttempts,
            RetryIntervalMinutes = step.RetryIntervalMinutes,
            ExecutionConditionExpression = step.ExecutionConditionExpression.Expression,
            StepTagIds = step.Tags.Select(t => t.TagId).ToArray(),
            TimeoutMinutes = step.TimeoutMinutes,
            DatabricksWorkspaceId = step.DatabricksWorkspaceId,
            DatabricksStepSettings = step.DatabricksStepSettings,
            Dependencies = dependencies,
            ExecutionConditionParameters = executionConditionParameters,
            Sources = step.DataObjects
                .Where(x => x.ReferenceType == DataObjectReferenceType.Source)
                .Select(x => new DataObjectRelation(x.DataObject.ObjectId, x.DataAttributes.ToArray()))
                .ToArray(),
            Targets = step.DataObjects
                .Where(x => x.ReferenceType == DataObjectReferenceType.Target)
                .Select(x => new DataObjectRelation(x.DataObject.ObjectId, x.DataAttributes.ToArray()))
                .ToArray(),
            Parameters = parameters
        };
        return await Mediator.SendAsync(command);
    }

    protected override async Task<DatabricksStep> OnSubmitUpdateAsync(DatabricksStep step)
    {
        switch (step.DatabricksStepSettings)
        {
            // Store the pipeline or job name only for audit purposes.
            case DbPipelineStepSettings pipeline:
                pipeline.PipelineName ??= _pipelines
                    ?.FirstOrDefault(p => p.PipelineId == pipeline.PipelineId)
                    ?.Name;
                break;
            case DbJobStepSettings job:
                job.JobName ??= _dbJobs
                    ?.FirstOrDefault(j => j.JobId == job.JobId)
                    ?.JobName;
                break;
            // Also store the cluster name if applicable.
            case DatabricksClusterStepSettings { ClusterConfiguration: ExistingClusterConfiguration existing }:
                existing.ClusterName ??= _clusters
                    ?.FirstOrDefault(c => c.ClusterId == existing.ClusterId)
                    ?.ClusterName;
                break;
        }
        var dependencies = step.Dependencies.ToDictionary(
            key => key.DependantOnStepId,
            value => value.DependencyType);
        var executionConditionParameters = step.ExecutionConditionParameters
            .Select(p => new UpdateExecutionConditionParameter(
                p.ParameterId,
                p.ParameterName,
                p.ParameterValue,
                p.JobParameterId))
            .ToArray();
        var parameters = step.StepParameters
            .Select(p => new UpdateStepParameter(
                p.ParameterId,
                p.ParameterName,
                p.ParameterValue,
                p.UseExpression,
                p.Expression.Expression,
                p.InheritFromJobParameterId,
                p.ExpressionParameters
                    .Select(e => new UpdateExpressionParameter(
                        e.ParameterId,
                        e.ParameterName,
                        e.InheritFromJobParameterId))
                    .ToArray()))
            .ToArray();
        var command = new UpdateDatabricksStepCommand
        {
            StepId = step.StepId,
            StepName = step.StepName ?? "",
            StepDescription = step.StepDescription,
            ExecutionPhase = step.ExecutionPhase,
            DuplicateExecutionBehaviour = step.DuplicateExecutionBehaviour,
            IsEnabled = step.IsEnabled,
            RetryAttempts = step.RetryAttempts,
            RetryIntervalMinutes = step.RetryIntervalMinutes,
            ExecutionConditionExpression = step.ExecutionConditionExpression.Expression,
            StepTagIds = step.Tags.Select(t => t.TagId).ToArray(),
            TimeoutMinutes = step.TimeoutMinutes,
            DatabricksWorkspaceId = step.DatabricksWorkspaceId,
            DatabricksStepSettings = step.DatabricksStepSettings,
            Dependencies = dependencies,
            ExecutionConditionParameters = executionConditionParameters,
            Sources = step.DataObjects
                .Where(x => x.ReferenceType == DataObjectReferenceType.Source)
                .Select(x => new DataObjectRelation(x.DataObject.ObjectId, x.DataAttributes.ToArray()))
                .ToArray(),
            Targets = step.DataObjects
                .Where(x => x.ReferenceType == DataObjectReferenceType.Target)
                .Select(x => new DataObjectRelation(x.DataObject.ObjectId, x.DataAttributes.ToArray()))
                .ToArray(),
            Parameters = parameters
        };
        return await Mediator.SendAsync(command);
    }

    private Task OpenFileSelectOffcanvas()
    {
        ArgumentNullException.ThrowIfNull(Step);
        return _fileSelectOffcanvas.LetAsync(x => x.ShowAsync(Step.DatabricksWorkspaceId));
    }

    private void OnFileSelected(string filePath)
    {
        ArgumentNullException.ThrowIfNull(Step);
        switch (Step.DatabricksStepSettings)
        {
            case DbNotebookStepSettings notebookSettings:
                notebookSettings.NotebookPath = filePath;
                break;
            case DbPythonFileStepSettings pythonSettings:
                pythonSettings.FilePath = filePath;
                break;
        }
    }

    private void OnWorkspaceChanged()
    {
        _runtimeVersions = null;
        _nodeTypes = null;
        _clusters = null;
        _dbJobs = null;
        _pipelines = null;
    }

    private async Task<Pipeline?> ResolvePipelineFromValueAsync(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        if (_pipelines is not null)
        {
            return _pipelines.FirstOrDefault(v => v.PipelineId == value);
        }
        
        try
        {
            var workspace = CurrentWorkspace;
            ArgumentNullException.ThrowIfNull(workspace);
            using var client = workspace.CreateClient();
            return await client.GetPipelineAsync(value);
        }
        catch (Exception ex)
        {
            Toaster.AddWarning("Error fetching Databricks pipeline", ex.Message);
            return null;
        }
    }

    private async Task<AutosuggestDataProviderResult<Pipeline>> ProvidePipelineSuggestionsAsync(
        AutosuggestDataProviderRequest request)
    {
        if (_pipelines is not null)
            return new()
            {
                Data = _pipelines.Where(n => n.Name.ContainsIgnoreCase(request.UserInput))
            };
        
        try
        {
            var workspace = CurrentWorkspace;
            ArgumentNullException.ThrowIfNull(workspace);
            using var client = workspace.CreateClient();
            var pipelines = await client.GetPipelinesAsync();
            _pipelines = pipelines.ToArray();
        }
        catch (Exception ex)
        {
            Toaster.AddError("Error fetching Databricks pipelines", ex.Message);
            _pipelines = [];
        }

        return new()
        {
            Data = _pipelines.Where(n => n.Name.ContainsIgnoreCase(request.UserInput))
        };
    }

    private async Task<DatabricksJob?> ResolveDbJobFromValueAsync(long value)
    {
        if (value <= 0)
        {
            return null;
        }

        if (_dbJobs is not null)
        {
            return _dbJobs.FirstOrDefault(v => v.JobId == value);
        }
        
        try
        {
            var workspace = CurrentWorkspace;
            ArgumentNullException.ThrowIfNull(workspace);
            using var client = workspace.CreateClient();
            return await client.GetJobAsync(value);
        }
        catch (Exception ex)
        {
            Toaster.AddWarning("Error fetching Databricks job", ex.Message);
            return null;
        }
    }

    private async Task<AutosuggestDataProviderResult<DatabricksJob>> ProvideDbJobSuggestionsAsync(
        AutosuggestDataProviderRequest request)
    {
        if (_dbJobs is not null)
            return new()
            {
                Data = _dbJobs.Where(n => n.JobName.ContainsIgnoreCase(request.UserInput))
            };
        
        try
        {
            var workspace = CurrentWorkspace;
            ArgumentNullException.ThrowIfNull(workspace);
            using var client = workspace.CreateClient();
            _dbJobs = (await client.GetJobsAsync()).ToArray();
        }
        catch (Exception ex)
        {
            Toaster.AddError("Error fetching Databricks jobs", ex.Message);
            _dbJobs = [];
        }

        return new()
        {
            Data = _dbJobs.Where(n => n.JobName.ContainsIgnoreCase(request.UserInput))
        };
    }

    private async Task<(string Id, string Description)> ResolveRuntimeVersionFromValueAsync(string? value)
    {
        if (_runtimeVersions is not null)
        {
            return _runtimeVersions.FirstOrDefault(v => v.Id == value);
        }
        
        try
        {
            var workspace = CurrentWorkspace;
            ArgumentNullException.ThrowIfNull(workspace);
            using var client = workspace.CreateClient();
            var runtimeVersions = await client.GetRuntimeVersionsAsync();
            _runtimeVersions = runtimeVersions.Select(v => (v.Key, v.Value)).ToArray();
        }
        catch (Exception ex)
        {
            Toaster.AddError("Error fetching available runtimes", ex.Message);
            _runtimeVersions = [];
        }
        
        return _runtimeVersions.FirstOrDefault(v => v.Id == value);
    }

    private async Task<AutosuggestDataProviderResult<(string Id, string Description)>> ProvideRuntimeVersionSuggestionsAsync(
        AutosuggestDataProviderRequest request)
    {
        if (_runtimeVersions is null)
        {
            try
            {
                var workspace = CurrentWorkspace;
                ArgumentNullException.ThrowIfNull(workspace);
                using var client = workspace.CreateClient();
                var runtimeVersions = await client.GetRuntimeVersionsAsync();
                _runtimeVersions = runtimeVersions.Select(v => (v.Key, v.Value)).ToArray();
            }
            catch (Exception ex)
            {
                Toaster.AddError("Error fetching available runtimes", ex.Message);
                _runtimeVersions = [];
            }
        }
        
        return new()
        {
            Data = _runtimeVersions
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
        var workspace = CurrentWorkspace;
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
        if (_nodeTypes is not null)
        {
            return _nodeTypes.FirstOrDefault(v => v.Id == value);
        }
        
        try
        {
            _nodeTypes = await GetNodeTypesAsync();
        }
        catch (Exception ex)
        {
            Toaster.AddError("Error fetching available node types", ex.Message);
            _nodeTypes = [];
        }
        
        return _nodeTypes.FirstOrDefault(v => v.Id == value);
    }

    private async Task<AutosuggestDataProviderResult<(string Id, string Description)>> ProvideNodeTypeSuggestionsAsync(
        AutosuggestDataProviderRequest request)
    {
        if (_nodeTypes is not null)
            return new()
            {
                Data = _nodeTypes.Where(n => n.Description.ContainsIgnoreCase(request.UserInput))
            };
        
        try
        {
            _nodeTypes = await GetNodeTypesAsync();
        }
        catch (Exception ex)
        {
            Toaster.AddError("Error fetching available node types", ex.Message);
            _nodeTypes = [];
        }

        return new()
        {
            Data = _nodeTypes.Where(n => n.Description.ContainsIgnoreCase(request.UserInput))
        };
    }

    private async Task<ClusterInfo?> ResolveClusterFromValueAsync(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        if (_clusters is not null)
        {
            return _clusters.FirstOrDefault(v => v.ClusterId == value);
        }
        
        try
        {
            var workspace = CurrentWorkspace;
            ArgumentNullException.ThrowIfNull(workspace);
            using var client = workspace.CreateClient();
            return await client.GetClusterAsync(value);
        }
        catch (Exception ex)
        {
            Toaster.AddWarning("Error fetching cluster information", ex.Message);
            return null;
        }
    }

    private async Task<AutosuggestDataProviderResult<ClusterInfo>> ProvideClusterSuggestionsAsync(
        AutosuggestDataProviderRequest request)
    {
        if (_clusters is not null)
            return new()
            {
                Data = _clusters.Where(n => n.ClusterName.ContainsIgnoreCase(request.UserInput))
            };
        
        try
        {
            var workspace = CurrentWorkspace;
            ArgumentNullException.ThrowIfNull(workspace);
            using var client = workspace.CreateClient();
            var clusters = await client.GetClustersAsync();
            _clusters = clusters
                .Where(c => c.ClusterSource == Microsoft.Azure.Databricks.Client.Models.ClusterSource.UI)
                .OrderBy(c => c.ClusterName)
                .ToArray();
        }
        catch (Exception ex)
        {
            Toaster.AddError("Error fetching available clusters", ex.Message);
            _clusters = [];
        }

        return new()
        {
            Data = _clusters.Where(n => n.ClusterName.ContainsIgnoreCase(request.UserInput))
        };
    }
}
