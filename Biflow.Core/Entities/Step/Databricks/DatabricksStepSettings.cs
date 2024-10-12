using System.Text.Json.Serialization;

namespace Biflow.Core.Entities;

[JsonDerivedType(typeof(DbNotebookStepSettings), "Notebook")]
[JsonDerivedType(typeof(DbPythonFileStepSettings), "PythonFile")]
[JsonDerivedType(typeof(DbJobStepSettings), "Job")]
[JsonDerivedType(typeof(DbPipelineStepSettings), "Pipeline")]
public abstract class DatabricksStepSettings;

public abstract class DatabricksClusterStepSettings : DatabricksStepSettings
{
    public ClusterConfiguration ClusterConfiguration { get; set; } = new NewClusterConfiguration();
}

public class DbNotebookStepSettings : DatabricksClusterStepSettings
{
    public string NotebookPath { get; set; } = "";
}

public class DbPythonFileStepSettings : DatabricksClusterStepSettings
{
    public string FilePath { get; set; } = "";
}

public class DbPipelineStepSettings : DatabricksStepSettings
{
    public string PipelineId { get; set; } = "";

    /// <summary>
    /// The pipeline name is stored only for audit purposes
    /// so that it can be viewed in the execution logs
    /// without having to navigate to the actual Databricks workspace.
    /// </summary>
    public string? PipelineName { get; set; }

    public bool PipelineFullRefresh { get; set; }
}

public class DbJobStepSettings : DatabricksStepSettings
{
    public long JobId { get; set; }

    /// <summary>
    /// The job name is stored only for audit purposes
    /// so that it can be viewed in the execution logs
    /// without having to navigate to the actual Databricks workspace.
    /// </summary>
    public string? JobName { get; set; }
}