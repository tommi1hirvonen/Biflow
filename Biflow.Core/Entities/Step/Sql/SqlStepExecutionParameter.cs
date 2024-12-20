namespace Biflow.Core.Entities;

public class SqlStepExecutionParameter : StepExecutionParameterBase
{
    public SqlStepExecutionParameter(string parameterName, ParameterValue parameterValue)
        : base(parameterName, parameterValue, ParameterType.Sql)
    {

    }

    public SqlStepExecutionParameter(SqlStepParameter parameter, SqlStepExecution execution) : base(parameter, execution)
    {
        StepExecution = execution;
    }

    public SqlStepExecution StepExecution { get; init; } = null!;

    public override StepExecution BaseStepExecution => StepExecution;
}
