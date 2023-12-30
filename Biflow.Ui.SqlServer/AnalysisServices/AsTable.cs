namespace Biflow.Ui.SqlServer;

public record AsTable(string TableName, AsModel Model, IEnumerable<AsPartition> Partitions);
