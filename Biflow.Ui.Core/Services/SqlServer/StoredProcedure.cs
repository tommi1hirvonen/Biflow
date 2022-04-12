namespace Biflow.Ui.Core;

public class StoredProcedure
{
    public StoredProcedure(int procedureId, string schemaName, string procedureName)
    {
        ProcedureId = procedureId;
        SchemaName = schemaName;
        ProcedureName = procedureName;
    }
    public int ProcedureId { get; }
    public string SchemaName { get; }
    public string ProcedureName { get; }
    public List<StoredProcedureParameter> Parameters { get; } = new();
}
