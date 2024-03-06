namespace Biflow.Executor.Core.ExecutionValidation;

internal interface IExecutionValidator
{
    public Task<bool> ValidateAsync(
        Execution execution,
        Func<string, Task> onValidationFailed,
        CancellationToken cancellationToken);
}
