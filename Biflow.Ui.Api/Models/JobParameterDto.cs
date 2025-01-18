namespace Biflow.Ui.Api.Models;

public record JobParameterDto(
    string ParameterName,
    ParameterValue? ParameterValue,
    bool UseExpression,
    string? Expression);