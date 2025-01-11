namespace Biflow.Ui.Api.Exceptions;

public class PrimaryKeyException<T> : PrimaryKeyException
{
    public PrimaryKeyException(Guid id) : base($"Object of type {typeof(T).Name} with id {id} already exists.")
    {
    }

    public PrimaryKeyException(int id) : base($"Object of type {typeof(T).Name} with id {id} already exists.")
    {
    }
}

public class PrimaryKeyException(string message) : Exception(message);