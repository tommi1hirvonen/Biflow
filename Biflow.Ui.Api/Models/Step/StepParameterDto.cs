namespace Biflow.Ui.Api.Models.Step;

[PublicAPI]
public record StepParameterDto(
    Guid? ParameterId,
    string ParameterName,
    ParameterValue ParameterValue,
    bool UseExpression,
    string? Expression,
    Guid? InheritFromJobParameterId,
    ExpressionParameterDto[] ExpressionParameters);
    
[PublicAPI]
public record PackageStepParameterDto(
    Guid? ParameterId,
    string ParameterName,
    ParameterLevel ParameterLevel,
    ParameterValue ParameterValue,
    bool UseExpression,
    string? Expression,
    Guid? InheritFromJobParameterId,
    ExpressionParameterDto[] ExpressionParameters);

[PublicAPI]
public record ExpressionParameterDto(Guid? ParameterId, string ParameterName, Guid InheritFromJobParameterId);