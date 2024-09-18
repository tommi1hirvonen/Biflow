namespace Biflow.Ui.SqlMetadataExtensions;

public class ObjectNotFoundException : Exception
{
    public ObjectNotFoundException(string objectName) : base($"The object {objectName} was not found.")
    {
    }

    public ObjectNotFoundException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}
