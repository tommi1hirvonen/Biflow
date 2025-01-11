namespace Biflow.Ui.Core;

public class NotFoundException<T> : NotFoundException
{
    public NotFoundException(Guid id) : base($"Object of type {typeof(T).Name} with id {id} was not found.")
    {
    }

    public NotFoundException(int id) : base($"Object of type {typeof(T).Name} with id {id} was not found.")
    {
    }

    public NotFoundException(params (string Field, Guid Value)[] key)
        : base($"Object of type {typeof(T).Name} with key {{ " +
               string.Join(", ", key.Select(x => $"{x.Field}={x.Value}")) +
               " } was not found.")
    {
    }
}

public class NotFoundException(string message) : Exception(message);