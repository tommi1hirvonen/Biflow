using Biflow.DataAccess.Models;

namespace Biflow.Ui.SqlServer;

public record DbObjectReference(string ServerName, string DatabaseName, string SchemaName, string ObjectName, bool IsUnreliable) : IDataObject
{
    public string ObjectUri { get; } = DataObject.CreateTableUri(ServerName, DatabaseName, SchemaName, ObjectName);
}