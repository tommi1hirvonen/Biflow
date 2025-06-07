using JetBrains.Annotations;

namespace Biflow.Core.Entities;

[PublicAPI]
public readonly record struct StepExecutionAttemptReference(Guid ExecutionId, Guid StepId, int RetryAttemptIndex);