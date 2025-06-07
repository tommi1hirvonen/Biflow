namespace Biflow.Ui.Api.Models;

[PublicAPI]
public record VersionRevertResponse(
    Guid Id,
    VersionRevertJobStatus Status,
    VersionRevertResponseIntegration[] NewIntegrations);

[PublicAPI]
public record VersionRevertResponseIntegration(string Type, string Name);
    