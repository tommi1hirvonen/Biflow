namespace Biflow.Proxy.WebApp;

public record Todo(int Id, string? Title, DateOnly? DueBy = null, bool IsComplete = false);