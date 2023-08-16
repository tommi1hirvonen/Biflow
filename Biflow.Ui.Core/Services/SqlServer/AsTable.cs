namespace Biflow.Ui.Core;

public record AsTable(string TableName, AsModel Model, IEnumerable<AsPartition> Partitions);
