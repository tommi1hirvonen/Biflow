namespace Biflow.Ui.Core;

internal class NotFoundException<T>(Guid id)
    : Exception($"Object of type {typeof(T).Name} with id {id} was not found.");