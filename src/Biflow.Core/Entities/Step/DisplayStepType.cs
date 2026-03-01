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
    Wait,
    Tabular,
    Vm,
    VmEnsureRunning,
    VmEnsureStopped,
    Unknown
}

public static class DisplayStepTypeExtensions
{
    extension(DisplayStepType)
    {
        public static DisplayStepType Parse(
            StepType stepType,
            FabricItemType? fabricItemType,
            VmStepOperation? vmOperation,
            DatabricksStepSettings? databricksSettings,
            QlikStepSettings? qlikSettings) => 
            (stepType, fabricItemType, vmOperation, databricksSettings, qlikSettings) switch
            {
                (StepType.AgentJob, _, _, _, _) => DisplayStepType.AgentJob,
                (StepType.Databricks, _, _, DbNotebookStepSettings, _) => DisplayStepType.DatabricksNotebook,
                (StepType.Databricks, _, _, DbSqlNotebookStepSettings, _) => DisplayStepType.DatabricksNotebook,
                (StepType.Databricks, _, _, DbPythonFileStepSettings, _) => DisplayStepType.DatabricksPythonFile,
                (StepType.Databricks, _, _, DbJobStepSettings, _) => DisplayStepType.DatabricksJob,
                (StepType.Databricks, _, _, DbPipelineStepSettings, _) => DisplayStepType.DatabricksPipeline,
                (StepType.Databricks, _, _, _, _) => DisplayStepType.Databricks,
                (StepType.Dataflow, _, _, _, _) => DisplayStepType.Dataflow,
                (StepType.Dataset, _, _, _, _) => DisplayStepType.Dataset,
                (StepType.Dbt, _, _, _, _) => DisplayStepType.Dbt,
                (StepType.Email, _, _, _, _) => DisplayStepType.Email,
                (StepType.Exe, _, _, _, _) => DisplayStepType.Exe,
                (StepType.Fabric, FabricItemType.Notebook, _, _, _) => DisplayStepType.FabricNotebook,
                (StepType.Fabric, FabricItemType.DataPipeline, _, _, _) => DisplayStepType.FabricPipeline,
                (StepType.Fabric, _, _, _, _) => DisplayStepType.Fabric,
                (StepType.Function, _, _, _, _) => DisplayStepType.Function,
                (StepType.Http, _, _, _, _) => DisplayStepType.Http, 
                (StepType.Job, _, _, _, _) => DisplayStepType.Job,
                (StepType.Package, _, _, _, _) => DisplayStepType.Package,
                (StepType.Pipeline, _, _, _, _) => DisplayStepType.Pipeline,
                (StepType.Qlik, _, _, _, QlikAppReloadSettings) => DisplayStepType.QlikApp,
                (StepType.Qlik, _, _, _, QlikAutomationRunSettings) => DisplayStepType.QlikAutomation,
                (StepType.Qlik, _, _, _, _) => DisplayStepType.Qlik,
                (StepType.Scd, _, _, _, _) => DisplayStepType.Scd,
                (StepType.Sql, _, _, _, _) => DisplayStepType.Sql,
                (StepType.Wait, _, _, _, _) => DisplayStepType.Wait,
                (StepType.Tabular, _, _, _, _) => DisplayStepType.Tabular,
                (StepType.Vm, _, VmStepOperation.EnsureRunning, _, _) => DisplayStepType.VmEnsureRunning,
                (StepType.Vm, _, VmStepOperation.EnsureStopped, _, _) => DisplayStepType.VmEnsureStopped,
                (StepType.Vm, _, _, _, _) => DisplayStepType.Vm,
                _ => DisplayStepType.Unknown
            };
    }
}