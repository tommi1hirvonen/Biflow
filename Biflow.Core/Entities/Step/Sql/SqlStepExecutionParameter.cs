using System.Text.Json.Serialization;

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

    [JsonIgnore]
    public SqlStepExecution StepExecution { get; init; } = null!;

    [JsonIgnore]
    public override StepExecution BaseStepExecution => StepExecution;
}
