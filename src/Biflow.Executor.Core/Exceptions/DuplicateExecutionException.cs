namespace Biflow.Executor.Core.Exceptions;

internal class DuplicateExecutionException(Guid executionId) : Exception($"Execution with id {executionId} is already being managed.")
{
    public Guid ExecutionId { get; } = executionId;
}
