namespace Biflow.Executor.Core.Exceptions;

public class ExecutionNotFoundException : Exception
{
    public ExecutionNotFoundException(Guid executionId) : base($"No execution found for id {executionId}")
    {
        ExecutionId = executionId;
    }

    public ExecutionNotFoundException(Guid executionId, string message) : base(message)
    {
        ExecutionId = executionId;
    }

    public Guid ExecutionId { get; }
}