namespace Biflow.Proxy.WebApp;

internal class TaskNotFoundException(Guid id) : Exception($"Proxy task with id {id} not found");