namespace Biflow.Ui.Api.Models;

[PublicAPI]
public record DependencyDto
{
    public required Guid DependentOnStepId { get; init; }
    public required DependencyType DependencyType { get; init; }
}