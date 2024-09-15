namespace Biflow.Ui.SqlMetadataExtensions;

public record DbTable(string Schema, string Table, bool HasPrimaryKey);