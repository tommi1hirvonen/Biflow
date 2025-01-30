namespace Biflow.Ui.Api.Models.Step;

[PublicAPI]
public record TabularStepDto : StepDto
{
    public required double TimeoutMinutes { get; init; }
    public required Guid ConnectionId { get; init; }
    public required string ModelName { get; init; }
    public required string? TableName { get; init; }
    public required string? PartitionName { get; init; }
}