namespace EtlManager.DataAccess.Models;

public class ExecutionSourceTargetObject
{
    public Guid ExecutionId { get; set; }

    public Guid ObjectId { get; set; }

    public string ServerName { get; set; } = string.Empty;

    public string DatabaseName { get; set; } = string.Empty;

    public string SchemaName { get; set; } = string.Empty;

    public string ObjectName { get; set; } = string.Empty;

    public int MaxConcurrentWrites { get; set; } = 1;

    public IList<StepExecution> Targets { get; set; } = null!;

    public IList<StepExecution> Sources { get; set; } = null!;
}