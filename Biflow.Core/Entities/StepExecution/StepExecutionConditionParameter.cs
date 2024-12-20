namespace Biflow.Core.Entities;

public sealed class StepExecutionConditionParameter : ParameterBase
{
    public StepExecutionConditionParameter(string parameterName, ParameterValue parameterValue)
    {
        ParameterName = parameterName;
        ParameterValue = parameterValue;
    }

    public StepExecutionConditionParameter(ExecutionConditionParameter parameter, StepExecution execution)
    {
        ExecutionId = execution.ExecutionId;
        StepId = parameter.StepId;
        ParameterName = parameter.ParameterName;
        ParameterId = parameter.ParameterId;
        ParameterValue = parameter.ParameterValue;
        ExecutionParameterId = parameter.JobParameterId;
        ExecutionParameter = execution.Execution.ExecutionParameters.FirstOrDefault(p => p.ParameterId == parameter.JobParameterId);
        StepExecution = execution;
    }

    public Guid ExecutionId { get; init; }

    public Guid StepId { get; init; }

    public override ParameterValue ParameterValue
    {
        get => ExecutionParameter is not null ? ExecutionParameterValue : field;
        set;
    } = new();

    public Guid? ExecutionParameterId { get; init; }

    public ParameterValue ExecutionParameterValue { get; set; }

    public ExecutionParameter? ExecutionParameter { get; init; }

    public StepExecution StepExecution { get; init; } = null!;

    public override string DisplayValue => ExecutionParameter switch
    {
        not null => $"{ExecutionParameter.DisplayValue} (inherited from execution parameter {ExecutionParameter.DisplayName})",
        _ => base.DisplayValue
    };

    public override string DisplayValueType => ExecutionParameter?.DisplayValueType ?? base.DisplayValueType;
}
