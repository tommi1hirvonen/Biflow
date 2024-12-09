namespace Biflow.Core.Entities;

public record DataflowGroup(string Id, string Name, IEnumerable<Dataflow> Dataflows);