namespace Biflow.Ui.TableEditor;

public record Column(
    string Name,
    bool IsPrimaryKey,
    bool IsIdentity,
    bool IsComputed,
    bool IsLocked,
    bool IsHidden,
    string DbDatatype,
    string DbDatatypeDescription,
    string DbCreateDatatype,
    Type? Datatype,
    Lookup? Lookup)
{
    public bool IsEditable => !IsIdentity && !IsComputed && Datatype is not null;

    public bool IsNullable => !DbDatatype.ContainsIgnoreCase("not null");
}
