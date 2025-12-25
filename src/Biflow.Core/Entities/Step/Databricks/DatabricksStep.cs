using Biflow.Core.Interfaces;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Biflow.Core.Entities;

public class DatabricksStep : Step, IHasTimeout, IHasStepParameters<DatabricksStepParameter>
{
    [JsonConstructor]
    public DatabricksStep() : base(StepType.Databricks) { }

    private DatabricksStep(DatabricksStep other, Job? targetJob) : base(other, targetJob)
    {
        TimeoutMinutes = other.TimeoutMinutes;
        DatabricksWorkspaceId = other.DatabricksWorkspaceId;
        DatabricksWorkspace = other.DatabricksWorkspace;
        DatabricksStepSettings = other.DatabricksStepSettings;
        StepParameters = other.StepParameters
            .Select(p => new DatabricksStepParameter(p, this, targetJob))
            .ToList();
    }

    [Required]
    [Range(0, 2880)] // 48 hours
    public double TimeoutMinutes { get; set; }

    public DatabricksStepSettings DatabricksStepSettings { get; set; } = new DbNotebookStepSettings();

    [Required]
    public Guid DatabricksWorkspaceId { get; set; }

    [JsonIgnore]
    public DatabricksWorkspace? DatabricksWorkspace { get; init; }

    [ValidateComplexType]
    [JsonInclude]
    public IList<DatabricksStepParameter> StepParameters { get; private set; } = new List<DatabricksStepParameter>();

    [JsonIgnore]
    public override DisplayStepType DisplayStepType => DatabricksStepSettings switch
    {
        DbNotebookStepSettings => DisplayStepType.DatabricksNotebook,
        DbPythonFileStepSettings => DisplayStepType.DatabricksPythonFile,
        DbJobStepSettings => DisplayStepType.DatabricksJob,
        DbPipelineStepSettings => DisplayStepType.DatabricksPipeline,
        _ => DisplayStepType.Databricks
    };

    public override DatabricksStep Copy(Job? targetJob = null) => new(this, targetJob);

    public override StepExecution ToStepExecution(Execution execution) => new DatabricksStepExecution(this, execution);

    // Convenience methods to change the Databricks step type while retaining common settings properties.

    public void SetIsNotebook()
    {
        if (DatabricksStepSettings is DbNotebookStepSettings)
        {
            return;
        }
        var settings = new DbNotebookStepSettings();
        if (DatabricksStepSettings is DatabricksClusterStepSettings cluster)
        {
            settings.ClusterConfiguration = cluster.ClusterConfiguration;
        }
        if (DatabricksStepSettings is DbPythonFileStepSettings python)
        {
            settings.NotebookPath = python.FilePath;
        }
        DatabricksStepSettings = settings;
    }

    public void SetIsPythonFile()
    {
        if (DatabricksStepSettings is DbPythonFileStepSettings)
        {
            return;
        }
        var settings = new DbPythonFileStepSettings();
        if (DatabricksStepSettings is DatabricksClusterStepSettings cluster)
        {
            settings.ClusterConfiguration = cluster.ClusterConfiguration;
        }
        if (DatabricksStepSettings is DbNotebookStepSettings notebook)
        {
            settings.FilePath = notebook.NotebookPath;
        }
        DatabricksStepSettings = settings;
    }

    public void SetIsPipeline()
    {
        if (DatabricksStepSettings is DbPipelineStepSettings)
        {
            return;
        }
        DatabricksStepSettings = new DbPipelineStepSettings();
    }

    public void SetIsJob()
    {
        if (DatabricksStepSettings is DbJobStepSettings)
        {
            return;
        }
        DatabricksStepSettings = new DbJobStepSettings();
    }
}
