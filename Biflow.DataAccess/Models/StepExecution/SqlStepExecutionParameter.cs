namespace Biflow.DataAccess.Models;

public class SqlStepExecutionParameter : StepExecutionParameterBase
{
    public SqlStepExecutionParameter(string parameterName, object parameterValue, ParameterValueType parameterValueType)
        : base(parameterName, parameterValue, ParameterType.Sql, parameterValueType)
    {

    }

    public SqlStepExecution StepExecution { get; set; } = null!;

    public override StepExecution BaseStepExecution => StepExecution;
}
