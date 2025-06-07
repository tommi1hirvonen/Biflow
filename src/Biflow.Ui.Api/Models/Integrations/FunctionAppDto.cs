namespace Biflow.Ui.Api.Models.Integrations;

[PublicAPI]
public record FunctionAppDto
{
    public required string FunctionAppName { get; init; }
    public required string SubscriptionId { get; init; }
    public required string ResourceGroupName { get; init; }
    public required string ResourceName { get; init; }
    public required Guid AzureCredentialId { get; init; }
    public required int MaxConcurrentFunctionSteps { get; init; }
    public string? FunctionAppKey { get; init; }
}