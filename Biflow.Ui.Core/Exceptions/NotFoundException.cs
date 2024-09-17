namespace Biflow.Ui.Core;

internal class NotFoundException<T> : Exception
{
    public NotFoundException(Guid id) : base($"Object of type {typeof(T).Name} with id {id} was not found.")
    {

    }

    public NotFoundException(int id) : base($"Object of type {typeof(T).Name} with id {id} was not found.")
    {

    }
}