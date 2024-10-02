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

public class DbPipelineStepSettings : DatabricksClusterStepSettings
{
    public string PipelineId { get; set; } = "";

    public bool PipelineFullRefresh { get; set; }
}

public class DbJobStepSettings : DatabricksStepSettings
{
    public long JobId { get; set; }

    public bool PipelineFullRefresh { get; set; }
}