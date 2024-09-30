namespace Biflow.Core.Entities;

public class DbNotebookStepExecutionParameter : StepExecutionParameterBase
{
    public DbNotebookStepExecutionParameter(string parameterName, ParameterValue parameterValue)
        : base(parameterName, parameterValue, ParameterType.DatabricksNotebook)
    {

    }

    public DbNotebookStepExecutionParameter(DbNotebookStepParameter parameter, DbNotebookStepExecution execution) : base(parameter, execution)
    {
        StepExecution = execution;
    }

    public DbNotebookStepExecution StepExecution { get; set; } = null!;

    public override StepExecution BaseStepExecution => StepExecution;
}