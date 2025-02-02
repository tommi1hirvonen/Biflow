namespace Biflow.Ui.Api.Models.Integrations;

[PublicAPI]
public record DataFactoryDto
{
    public required string PipelineClientName { get; init; }
    public required int MaxConcurrentPipelineSteps { get; init; }
    public required Guid AzureCredentialId { get; init; }
    public required string SubscriptionId { get; init; }
    public required string ResourceGroupName { get; init; }
    public required string ResourceName { get; init; }
}