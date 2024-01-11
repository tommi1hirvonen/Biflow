namespace Biflow.DataAccess.Models;

public record QlikAppReload(string Id, QlikAppReloadStatus Status, string? Log);
