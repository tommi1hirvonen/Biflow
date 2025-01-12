namespace Biflow.Ui.Api.Models;

[PublicAPI]
public record VersionRevertResponse(Guid Id, VersionRevertStatus Status);