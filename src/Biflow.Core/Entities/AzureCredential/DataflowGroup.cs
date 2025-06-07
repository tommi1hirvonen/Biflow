using JetBrains.Annotations;

namespace Biflow.Core.Entities;

[PublicAPI]
public record DataflowGroup(string Id, string Name, IEnumerable<Dataflow> Dataflows);