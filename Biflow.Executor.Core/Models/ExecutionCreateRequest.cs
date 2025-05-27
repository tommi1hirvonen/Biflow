namespace Biflow.Executor.Core.Models;

[PublicAPI]
internal record ExecutionCreateRequest(Guid JobId, Guid[]? StepIds, bool? StartExecution);