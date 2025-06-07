namespace Biflow.Executor.Core;

public class OrchestrationContext(Guid executionId, Guid? parentExecutionId, bool synchronizedExecution)
{
    public Guid ExecutionId { get; } = executionId;
    
    public Guid? ParentExecutionId { get; } = parentExecutionId;
    
    public bool SynchronizedExecution { get; } = synchronizedExecution;
}