namespace Biflow.Ui.TableEditor;

internal class PrimaryKeyNotFoundException(string column) : Exception($"Primary key column {column} not found");