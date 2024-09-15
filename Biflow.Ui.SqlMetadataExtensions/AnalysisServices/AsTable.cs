namespace Biflow.Ui.SqlMetadataExtensions;

public record AsTable(string TableName, AsModel Model, IEnumerable<AsPartition> Partitions);
