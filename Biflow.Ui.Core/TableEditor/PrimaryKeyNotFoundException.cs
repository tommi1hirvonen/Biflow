namespace Biflow.Ui.Core;

internal class PrimaryKeyNotFoundException(string column) : Exception($"Primary key column {column} not found");