namespace Biflow.Ui.SqlMetadataExtensions;

public record DbObject(string Server, string Database, string Schema, string Object, string Type)
{
    public override string ToString() => $"[{Schema}].[{Object}] ({Type})";
}