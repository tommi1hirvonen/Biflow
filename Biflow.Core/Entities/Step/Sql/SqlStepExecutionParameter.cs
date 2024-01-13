namespace Biflow.Core.Entities;

public class SqlStepExecutionParameter : StepExecutionParameterBase
{
    public SqlStepExecutionParameter(string parameterName, object parameterValue, ParameterValueType parameterValueType)
        : base(parameterName, parameterValue, ParameterType.Sql, parameterValueType)
    {

    }

    public SqlStepExecutionParameter(SqlStepParameter parameter, SqlStepExecution execution) : base(parameter, execution)
    {
        StepExecution = execution;
    }

    public SqlStepExecution StepExecution { get; set; } = null!;

    public override StepExecution BaseStepExecution => StepExecution;
}
