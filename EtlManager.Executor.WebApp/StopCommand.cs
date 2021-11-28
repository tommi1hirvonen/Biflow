namespace EtlManager.Executor.WebApp;

public class StopCommand
{
    public Guid ExecutionId { get; set; }

    public string Username { get; set; } = string.Empty;

    public Guid? StepId { get; set; }
}
