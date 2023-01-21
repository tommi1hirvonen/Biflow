using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;

namespace Biflow.DataAccess.Models;

[Table("ExecutionStepConditionParameter")]
[PrimaryKey("ExecutionId", "ParameterId")]
public class StepExecutionConditionParameter : ParameterBase
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

    [Column(TypeName = "sql_variant")]
    public override object? ParameterValue
    {
        get => ExecutionParameterValue ?? _parameterValue;
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

    [Column(TypeName = "sql_variant")]
    public object? ExecutionParameterValue { get; set; }

    public ExecutionParameter? ExecutionParameter { get; set; }

    public StepExecution StepExecution { get; set; } = null!;
}
