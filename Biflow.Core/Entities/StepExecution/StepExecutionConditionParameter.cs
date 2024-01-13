namespace Biflow.Core.Entities;

public class StepExecutionConditionParameter : ParameterBase
{
    public StepExecutionConditionParameter(string parameterName, object parameterValue, ParameterValueType parameterValueType)
    {
        ParameterName = parameterName;
        _parameterValue = parameterValue;
        ParameterValueType = parameterValueType;
    }

    public StepExecutionConditionParameter(ExecutionConditionParameter parameter, StepExecution execution)
    {
        ExecutionId = execution.ExecutionId;
        StepId = parameter.StepId;
        ParameterName = parameter.ParameterName;
        ParameterId = parameter.ParameterId;
        ParameterValue = parameter.ParameterValue;
        ParameterValueType = parameter.ParameterValueType;
        ExecutionParameterId = parameter.JobParameterId;
        ExecutionParameter = execution.Execution.ExecutionParameters.FirstOrDefault(p => p.ParameterId == parameter.JobParameterId);
        StepExecution = execution;
    }

    public Guid ExecutionId { get; set; }

    public Guid StepId { get; set; }

    public override object? ParameterValue
    {
        get => ExecutionParameter is not null ? ExecutionParameterValue : _parameterValue;
        set => _parameterValue = value;
    }

    private object? _parameterValue;

    public override ParameterValueType ParameterValueType
    {
        get => ExecutionParameter?.ParameterValueType ?? _parameterValueType;
        set => _parameterValueType = value;
    }

    private ParameterValueType _parameterValueType = ParameterValueType.String;

    public Guid? ExecutionParameterId { get; set; }

    public object? ExecutionParameterValue { get; set; }

    public ExecutionParameter? ExecutionParameter { get; set; }

    public StepExecution StepExecution { get; set; } = null!;

    public override string DisplayValue => ExecutionParameter switch
    {
        not null => $"{ExecutionParameter.DisplayValue?.ToString() ?? "null"} (inherited from execution parameter {ExecutionParameter.DisplayName})",
        _ => base.DisplayValue
    };

    public override string DisplayValueType => ExecutionParameter?.DisplayValueType ?? base.DisplayValueType;
}
