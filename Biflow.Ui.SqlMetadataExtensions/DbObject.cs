using Biflow.Core.Entities;
using Biflow.Core.Interfaces;

namespace Biflow.Ui.SqlMetadataExtensions;

public record DbObject(string Server, string Database, string Schema, string Object, string Type) : IDataObject
{
    public override string ToString() => $"[{Schema}].[{Object}] ({Type})";
    
    public string ObjectUri => DataObject.CreateTableUri(Server, Database, Schema, Object);
}