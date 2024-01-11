namespace Biflow.DataAccess.Models;

public record QlikSpace(string Id, string Name, IEnumerable<QlikApp> Apps);
