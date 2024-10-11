namespace Biflow.Core.Entities;

public record QlikAppReload(string Id, QlikAppReloadStatus Status, string? Log);
