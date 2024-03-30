namespace Biflow.Core.Entities;

public readonly record struct StepExecutionAttemptReference(Guid ExecutionId, Guid StepId, int RetryAttemptIndex);