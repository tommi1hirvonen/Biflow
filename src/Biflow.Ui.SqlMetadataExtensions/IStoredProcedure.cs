namespace Biflow.Ui.SqlMetadataExtensions;

public interface IStoredProcedure
{
    public string SchemaName { get; }

    public string ProcedureName { get; }

    public string QuotedSchemaName { get; }

    public string QuotedProcedureName { get; }

    public string ArgumentSignature { get; }

    public string InvokeSqlStatement { get; }

    public IEnumerable<IStoredProcedureParameter> Parameters { get; }
}