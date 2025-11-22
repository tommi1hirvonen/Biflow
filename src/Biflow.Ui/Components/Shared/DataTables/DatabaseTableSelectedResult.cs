namespace Biflow.Ui.Components.Shared.DataTables;

public record DatabaseTableSelectedResult(string Schema, string Name)
{
    public void Deconstruct(out string schema, out string name) => (schema, name) = (Schema, Name);
}
