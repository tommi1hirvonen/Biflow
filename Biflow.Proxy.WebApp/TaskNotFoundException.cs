namespace Biflow.Proxy.WebApp;

public class TaskNotFoundException(Guid id) : Exception($"Proxy task with id {id} not found");