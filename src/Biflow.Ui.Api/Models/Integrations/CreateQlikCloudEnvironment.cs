namespace Biflow.Ui.Api.Models.Integrations;

[PublicAPI]
public record CreateQlikCloudEnvironment
{
    public required string QlikCloudEnvironmentName { get; init; }
    public required string EnvironmentUrl { get; init; }
    public required string ApiToken { get; init; }
}