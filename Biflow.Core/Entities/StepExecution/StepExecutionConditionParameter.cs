namespace Biflow.Core.Entities;

public class StepExecutionConditionParameter : ParameterBase
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

    public Guid ExecutionId { get; set; }

    public Guid StepId { get; set; }

    public override ParameterValue ParameterValue
    {
        get => ExecutionParameter is not null ? ExecutionParameterValue : field;
        set => field = value;
    } = new();

    public Guid? ExecutionParameterId { get; set; }

    public ParameterValue ExecutionParameterValue { get; set; } = new();

    public ExecutionParameter? ExecutionParameter { get; set; }

    public StepExecution StepExecution { get; set; } = null!;

    public override string DisplayValue => ExecutionParameter switch
    {
        not null => $"{ExecutionParameter.DisplayValue?.ToString() ?? "null"} (inherited from execution parameter {ExecutionParameter.DisplayName})",
        _ => base.DisplayValue
    };

    public override string DisplayValueType => ExecutionParameter?.DisplayValueType ?? base.DisplayValueType;
}
