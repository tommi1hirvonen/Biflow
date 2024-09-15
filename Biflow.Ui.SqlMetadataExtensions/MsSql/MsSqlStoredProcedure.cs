namespace Biflow.Ui.SqlMetadataExtensions;

public class MsSqlStoredProcedure(int procedureId, string schemaName, string procedureName) : IStoredProcedure
{
    public int ProcedureId { get; } = procedureId;

    public string SchemaName { get; } = schemaName;

    public string ProcedureName { get; } = procedureName;

    public string QuotedSchemaName { get; } = $"[{schemaName}]";

    public string QuotedProcedureName { get; } = $"[{procedureName}]";

    public string ArgumentSignature { get; } = string.Empty;

    public string InvokeSqlStatement { get; } = $"EXEC [{schemaName}].[{procedureName}]";

    public List<MsSqlStoredProcedureParameter> Parameters { get; } = [];

    IEnumerable<IStoredProcedureParameter> IStoredProcedure.Parameters => Parameters;

    public override string ToString() => $"{QuotedSchemaName}.{QuotedProcedureName}";
}
