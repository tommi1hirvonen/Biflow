namespace Biflow.Ui.Api.Models.Step;

[PublicAPI]
public record DataflowStepDto : StepDto
{
    public required double TimeoutMinutes { get; init; }
    public required Guid AzureCredentialId { get; init; }
    public required Guid WorkspaceId { get; init; }
    public string? WorkspaceName { get; init; }
    public required Guid DataflowId { get; init; }
    public string? DataflowName { get; init; }
}