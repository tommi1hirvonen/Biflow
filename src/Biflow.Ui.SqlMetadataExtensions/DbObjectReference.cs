using Biflow.Core.Entities;
using Biflow.Core.Interfaces;

namespace Biflow.Ui.SqlMetadataExtensions;

public class DbObjectReference : IDataObject
{
    public required string ServerName { get; init; }
    
    public required string DatabaseName { get; init; }
    
    public required string SchemaName { get; init; }
    
    public required string ObjectName { get; init; }
    
    public required bool IsUnreliable { get; init; }
    
    public string ObjectUri => DataObject.CreateTableUri(ServerName, DatabaseName, SchemaName, ObjectName);
}