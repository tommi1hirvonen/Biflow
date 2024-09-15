namespace Biflow.Ui.SqlMetadataExtensions;

public class StoredProcedure(int procedureId, string schemaName, string procedureName)
{
    public int ProcedureId { get; } = procedureId;

    public string SchemaName { get; } = schemaName;

    public string ProcedureName { get; } = procedureName;

    public List<StoredProcedureParameter> Parameters { get; } = [];
}
