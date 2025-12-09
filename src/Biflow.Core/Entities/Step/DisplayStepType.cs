namespace Biflow.Core.Entities;

public enum DisplayStepType
{
    AgentJob,
    Databricks,
    DatabricksJob,
    DatabricksNotebook,
    DatabricksPipeline,
    DatabricksPythonFile,
    Dataflow,
    Dataset,
    Dbt,
    Email,
    Exe,
    Fabric,
    FabricNotebook,
    FabricPipeline,
    Function,
    Http,
    Job,
    Package,
    Pipeline,
    Qlik,
    QlikAutomation,
    QlikApp,
    Scd,
    Sql,
    Tabular,
    Unknown
}

public static class DisplayStepTypeExtensions
{
    extension(DisplayStepType)
    {
        public static DisplayStepType Parse(
            StepType stepType,
            FabricItemType? fabricItemType,
            DatabricksStepSettings? databricksSettings,
            QlikStepSettings? qlikSettings) => 
            (stepType, fabricItemType, databricksSettings, qlikSettings) switch
            {
                (StepType.AgentJob, _, _, _) => DisplayStepType.AgentJob,
                (StepType.Databricks, _, DbNotebookStepSettings, _) => DisplayStepType.DatabricksNotebook,
                (StepType.Databricks, _, DbPythonFileStepSettings, _) => DisplayStepType.DatabricksPythonFile,
                (StepType.Databricks, _, DbJobStepSettings, _) => DisplayStepType.DatabricksJob,
                (StepType.Databricks, _, DbPipelineStepSettings, _) => DisplayStepType.DatabricksPipeline,
                (StepType.Databricks, _, _, _) => DisplayStepType.Databricks,
                (StepType.Dataflow, _, _, _) => DisplayStepType.Dataflow,
                (StepType.Dataset, _, _, _) => DisplayStepType.Dataset,
                (StepType.Dbt, _, _, _) => DisplayStepType.Dbt,
                (StepType.Email, _, _, _) => DisplayStepType.Email,
                (StepType.Exe, _, _, _) => DisplayStepType.Exe,
                (StepType.Fabric, FabricItemType.Notebook, _, _) => DisplayStepType.FabricNotebook,
                (StepType.Fabric, FabricItemType.DataPipeline, _, _) => DisplayStepType.FabricPipeline,
                (StepType.Fabric, _, _, _) => DisplayStepType.Fabric,
                (StepType.Function, _, _, _) => DisplayStepType.Function,
                (StepType.Http, _, _, _) => DisplayStepType.Http, 
                (StepType.Job, _, _, _) => DisplayStepType.Job,
                (StepType.Package, _, _, _) => DisplayStepType.Package,
                (StepType.Pipeline, _, _, _) => DisplayStepType.Pipeline,
                (StepType.Qlik, _, _, QlikAppReloadSettings) => DisplayStepType.QlikApp,
                (StepType.Qlik, _, _, QlikAutomationRunSettings) => DisplayStepType.QlikAutomation,
                (StepType.Qlik, _, _, _) => DisplayStepType.Qlik,
                (StepType.Scd, _, _, _) => DisplayStepType.Scd,
                (StepType.Sql, _, _, _) => DisplayStepType.Sql,
                (StepType.Tabular, _, _, _) => DisplayStepType.Tabular,
                _ => DisplayStepType.Unknown
            };
    }
}