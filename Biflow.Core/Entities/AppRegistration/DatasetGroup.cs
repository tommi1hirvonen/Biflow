namespace Biflow.Core.Entities;

public record DatasetGroup(string Id, string Name, IEnumerable<Dataset> Datasets);