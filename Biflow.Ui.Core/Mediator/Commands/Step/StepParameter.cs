namespace Biflow.Ui.Core;

public record CreateStepParameter(
    string ParameterName,
    ParameterValue ParameterValue,
    bool UseExpression,
    string? Expression,
    Guid? InheritFromJobParameterId,
    CreateExpressionParameter[] ExpressionParameters);

public record CreatePackageStepParameter(
    string ParameterName,
    ParameterLevel ParameterLevel,
    ParameterValue ParameterValue,
    bool UseExpression,
    string? Expression,
    Guid? InheritFromJobParameterId,
    CreateExpressionParameter[] ExpressionParameters);
    
public record CreateExpressionParameter(
    string ParameterName,
    Guid InheritFromJobParameterId);

public record UpdateStepParameter(
    Guid? ParameterId,
    string ParameterName,
    ParameterValue ParameterValue,
    bool UseExpression,
    string? Expression,
    Guid? InheritFromJobParameterId,
    UpdateExpressionParameter[] ExpressionParameters);

public record UpdatePackageStepParameter(
    Guid? ParameterId,
    string ParameterName,
    ParameterLevel ParameterLevel,
    ParameterValue ParameterValue,
    bool UseExpression,
    string? Expression,
    Guid? InheritFromJobParameterId,
    UpdateExpressionParameter[] ExpressionParameters)
    : UpdateStepParameter(
        ParameterId,
        ParameterName,
        ParameterValue,
        UseExpression,
        Expression,
        InheritFromJobParameterId,
        ExpressionParameters);
    
public record UpdateExpressionParameter(
    Guid? ParameterId,
    string ParameterName,
    Guid InheritFromJobParameterId);