using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;

namespace Biflow.DataAccess.Models;

[Table("ExecutionStepParameter")]
[PrimaryKey("ExecutionId", "ParameterId")]
public abstract class StepExecutionParameterBase
{
    public StepExecutionParameterBase(string parameterName, object parameterValue, ParameterType parameterType, ParameterValueType parameterValueType)
    {
        ParameterName = parameterName;
        _parameterValue = parameterValue;
        ParameterValueType = parameterValueType;
        ParameterType = parameterType;
    }

    public Guid ExecutionId { get; set; }

    public Guid StepId { get; set; }

    public Guid ParameterId { get; set; }

    public ParameterType ParameterType { get; set; }

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
        get => InheritFromExecutionParameter?.ParameterValueType ?? _parameterValueType;
        set => _parameterValueType = value;
    }

    private ParameterValueType _parameterValueType = ParameterValueType.String;

    public Guid? InheritFromExecutionParameterId { get; set; }

    [Column(TypeName = "sql_variant")]
    public object? ExecutionParameterValue { get; set; }

    public ExecutionParameter? InheritFromExecutionParameter { get; set; }

    public abstract StepExecution BaseStepExecution { get; }
}
