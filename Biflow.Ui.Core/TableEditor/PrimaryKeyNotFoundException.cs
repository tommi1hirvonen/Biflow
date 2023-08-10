namespace Biflow.Ui.Core;

internal class PrimaryKeyNotFoundException : Exception
{
    public PrimaryKeyNotFoundException(string column) : base($"Primary key column {column} not found")
    {

    }
}
