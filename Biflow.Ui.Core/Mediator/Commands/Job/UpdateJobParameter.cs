namespace Biflow.Ui.Core;

public record UpdateJobParameter(
    Guid? ParameterId,
    string ParameterName,
    ParameterValue? ParameterValue,
    bool UseExpression,
    string? Expression);