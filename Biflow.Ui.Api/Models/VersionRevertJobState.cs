namespace Biflow.Ui.Api.Models;

public record VersionRevertJobState(VersionRevertJobStatus Status, (string Type, string Name)[] NewIntegrations);