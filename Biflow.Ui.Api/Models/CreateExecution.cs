namespace Biflow.Ui.Api.Models;

[PublicAPI]
public record CreateExecution
{
    public required Guid JobId { get; init; }
    public Guid[]? StepIds { get; init; }
}