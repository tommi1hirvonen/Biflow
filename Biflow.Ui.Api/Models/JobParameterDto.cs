namespace Biflow.Ui.Api.Models;

[PublicAPI]
public record JobParameterDto
{
    public required string ParameterName { get; init; }
    public required ParameterValue? ParameterValue { get; init; }
    public required bool UseExpression { get; init; }
    public required string? Expression { get; init; }
}