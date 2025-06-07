namespace Biflow.Ui.Core;

public record CreateExecutionConditionParameter(
    string ParameterName,
    ParameterValue ParameterValue,
    Guid? InheritFromJobParameterId);

public record UpdateExecutionConditionParameter(
    Guid? ParameterId,
    string ParameterName,
    ParameterValue ParameterValue,
    Guid? InheritFromJobParameterId);