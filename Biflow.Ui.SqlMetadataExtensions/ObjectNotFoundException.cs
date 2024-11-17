namespace Biflow.Ui.SqlMetadataExtensions;

public class ObjectNotFoundException(string objectName)
    : Exception($"The object {objectName} was not found.");
