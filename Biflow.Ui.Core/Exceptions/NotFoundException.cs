namespace Biflow.Ui.Core;

public class NotFoundException<T> : NotFoundException
{
    public NotFoundException(Guid id) : base($"Object of type {typeof(T).Name} with id {id} was not found.")
    {
    }

    public NotFoundException(int id) : base($"Object of type {typeof(T).Name} with id {id} was not found.")
    {
    }
}

public class NotFoundException(string message) : Exception(message);