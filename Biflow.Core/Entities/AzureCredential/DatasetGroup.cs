using JetBrains.Annotations;

namespace Biflow.Core.Entities;

[PublicAPI]
public record DatasetGroup(string Id, string Name, IEnumerable<Dataset> Datasets);