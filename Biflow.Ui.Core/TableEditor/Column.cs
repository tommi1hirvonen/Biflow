namespace Biflow.Ui.Core;

public class Column
{
    public string Name { get; }

    public bool IsPrimaryKey { get; }

    public bool IsIdentity { get; }

    public bool IsComputed { get; }

    public string DbDatatype { get; }

    public string DbDatatypeDescription { get; }

    public Type? Datatype { get; }

    public Lookup? Lookup { get; }

    public bool IsEditable => !IsIdentity && !IsComputed && Datatype is not null;

    public Column(
        string name,
        bool isPrimaryKey,
        bool isIdentity,
        bool isComputed,
        string dbDatatype,
        string dbDatatypeDescription,
        Type? datatype,
        Lookup? lookup)
    {
        Name = name;
        IsPrimaryKey = isPrimaryKey;
        IsIdentity = isIdentity;
        IsComputed = isComputed;
        DbDatatype = dbDatatype;
        DbDatatypeDescription = dbDatatypeDescription;
        Datatype = datatype;
        Lookup = lookup;
    }

    
}
