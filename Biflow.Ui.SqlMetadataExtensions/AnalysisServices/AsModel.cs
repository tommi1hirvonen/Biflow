namespace Biflow.Ui.SqlMetadataExtensions;

public record AsModel(string ModelName, IEnumerable<AsTable> Tables, AsServer Server);
