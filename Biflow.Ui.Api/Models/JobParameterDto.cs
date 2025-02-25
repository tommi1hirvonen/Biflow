namespace Biflow.Ui.Api.Models;

[PublicAPI]
public record JobParameterDto
{
    public Guid? ParameterId { get; init; }
    public required string ParameterName { get; init; }
    public required ParameterValue? ParameterValue { get; init; }
    public bool UseExpression { get; init; }
    public string? Expression { get; init; }
}