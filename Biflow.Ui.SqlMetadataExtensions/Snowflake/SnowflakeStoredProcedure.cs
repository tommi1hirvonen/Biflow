namespace Biflow.Ui.SqlMetadataExtensions;

public class SnowflakeStoredProcedure(string schemaName, string procedureName, string argumentSignature) : IStoredProcedure
{
    public string SchemaName { get; } = schemaName;

    public string ProcedureName { get; } = procedureName;

    public string ArgumentSignature { get; } = argumentSignature;

    public string QuotedSchemaName { get; } = $"\"{schemaName}\"";

    public string QuotedProcedureName { get; } = $"\"{procedureName}\"";

    public string InvokeSqlStatement
    {
        get
        {
            var parameters = string.Join(", ", Parameters.Select(p => $":{p.ParameterName}"));
            return $"""
                CALL "{SchemaName}"."{ProcedureName}"({parameters})
                """;
        }
    }

    public List<SnowflakeStoredProcedureParameter> Parameters { get; } = [];

    IEnumerable<IStoredProcedureParameter> IStoredProcedure.Parameters => Parameters;

    public override string ToString() => $"{QuotedSchemaName}.{QuotedProcedureName}{ArgumentSignature}";

    public override bool Equals(object? obj)
    {
        if (obj is SnowflakeStoredProcedure other)
        {
            return SchemaName == other.SchemaName && ProcedureName == other.ProcedureName && ArgumentSignature == other.ArgumentSignature;
        }
        return false;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(SchemaName, ProcedureName, ArgumentSignature);
    }
}
