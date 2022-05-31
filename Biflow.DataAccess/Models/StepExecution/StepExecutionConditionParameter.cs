using System.ComponentModel.DataAnnotations.Schema;

namespace Biflow.DataAccess.Models;

public class StepExecutionConditionParameter
{
    public StepExecutionConditionParameter(string parameterName, object parameterValue, ParameterValueType parameterValueType)
    {
        ParameterName = parameterName;
        _parameterValue = parameterValue;
        ParameterValueType = parameterValueType;
    }

    [Column("ExecutionId")]
    public Guid ExecutionId { get; set; }

    [Column("StepId")]
    public Guid StepId { get; set; }

    public Guid ParameterId { get; set; }

    public string ParameterName { get; set; }

    [Column(TypeName = "sql_variant")]
    public object ParameterValue
    {
        get => ExecutionParameterValue ?? _parameterValue;
        set => _parameterValue = value;
    }

    private object _parameterValue;

    public ParameterValueType ParameterValueType
    {
        get => ExecutionParameter?.ParameterValueType ?? _parameterValueType;
        set => _parameterValueType = value;
    }

    private ParameterValueType _parameterValueType = ParameterValueType.String;

    public Guid? ExecutionParameterId { get; set; }

    [Column(TypeName = "sql_variant")]
    public object? ExecutionParameterValue { get; set; }

    public ExecutionParameter? ExecutionParameter { get; set; }

    public StepExecution StepExecution { get; set; } = null!;
}
