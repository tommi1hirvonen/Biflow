namespace Biflow.Ui.Core;

public record AsModel(string ModelName, IEnumerable<AsTable> Tables, AsServer Server);
