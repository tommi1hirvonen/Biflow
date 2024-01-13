namespace Biflow.Core.Entities;

public record QlikSpace(string Id, string Name, IEnumerable<QlikApp> Apps);
