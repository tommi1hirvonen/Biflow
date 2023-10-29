namespace Biflow.DataAccess.Models;

public record DatasetGroup(string Id, string Name, IEnumerable<Dataset> Datasets);