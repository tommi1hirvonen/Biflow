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

    public IEnumerable<(object? Value, object? DisplayValue)>? LookupValues { get; }

    public bool IsEditable => !IsIdentity && !IsComputed && Datatype is not null;

    public Column(
        string name,
        bool isPrimaryKey,
        bool isIdentity,
        bool isComputed,
        string dbDatatype,
        string dbDatatypeDescription,
        IEnumerable<(object? Value, object? DisplayValue)>? lookupValues)
    {
        Name = name;
        IsPrimaryKey = isPrimaryKey;
        IsIdentity = isIdentity;
        IsComputed = isComputed;
        DbDatatype = dbDatatype;
        DbDatatypeDescription = dbDatatypeDescription;
        Datatype = DatatypeMapping.GetValueOrDefault(dbDatatype);
        LookupValues = lookupValues;
    }

    public static readonly Dictionary<string, Type> DatatypeMapping = new()
    {
        { "char", typeof(string) },
        { "varchar", typeof(string) },
        { "nchar", typeof(string) },
        { "nvarchar", typeof(string) },
        { "tinyint", typeof(byte) },
        { "smallint", typeof(short) },
        { "int", typeof(int) },
        { "bigint", typeof(long) },
        { "smallmoney", typeof(decimal) },
        { "money", typeof(decimal) },
        { "numeric", typeof(decimal) },
        { "decimal", typeof(decimal) },
        { "real", typeof(float) },
        { "float", typeof(double) },
        { "smalldatetime", typeof(DateTime) },
        { "datetime", typeof(DateTime) },
        { "datetime2", typeof(DateTime) },
        { "date", typeof(DateTime) },
        { "bit", typeof(bool) }
    };
}
