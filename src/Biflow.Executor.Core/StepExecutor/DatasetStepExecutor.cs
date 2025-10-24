using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Biflow.Executor.Core.StepExecutor;

internal class DatasetStepExecutor(
    IServiceProvider serviceProvider,
    DatasetStepExecution step,
    DatasetStepExecutionAttempt attempt) : IStepExecutor
{
    private readonly ILogger<DatasetStepExecutor> _logger = serviceProvider
        .GetRequiredService<ILogger<DatasetStepExecutor>>();
    private readonly int _pollingIntervalMs = serviceProvider
        .GetRequiredService<IOptionsMonitor<ExecutionOptions>>()
        .CurrentValue
        .PollingIntervalMs;
    private readonly DatasetClient _client = step
        .GetAzureCredential()
        ?.CreateDatasetClient(serviceProvider.GetRequiredService<ITokenService>())
        ?? throw new ArgumentNullException(message: "Azure credential was null", innerException: null);

    public async Task<Result> ExecuteAsync(OrchestrationContext context, ExtendedCancellationTokenSource cts)
    {
        var cancellationToken = cts.Token;
        cancellationToken.ThrowIfCancellationRequested();

        // Start dataset refresh.
        try
        {
            await _client.RefreshDatasetAsync(step.WorkspaceId, step.DatasetId, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting dataset refresh");
            attempt.AddError(ex, "Error starting dataset refresh operation");
            return Result.Failure;
        }

        // Wait for 5 seconds before first attempting to get the dataset refresh status.
        await Task.Delay(5000, cancellationToken);

        while (true)
        {
            try
            {
                var (status, refresh) = await _client.GetDatasetRefreshStatusAsync(step.WorkspaceId, step.DatasetId,
                    cancellationToken);
                switch (status)
                {
                    case DatasetRefreshStatus.Completed:
                        return Result.Success;
                    case DatasetRefreshStatus.Failed or DatasetRefreshStatus.Disabled:
                        attempt.AddError(refresh?.ServiceExceptionJson);
                        return Result.Failure;
                    default:
                        await Task.Delay(_pollingIntervalMs, cancellationToken);
                        break;
                }
            }
            catch (OperationCanceledException ex)
            {
                // If the token was canceled, report result as 'Cancel'.
                if (cancellationToken.IsCancellationRequested)
                {
                    attempt.AddWarning(ex);
                    return Result.Cancel;
                }
                // If not, report error. This means the step was not canceled, but instead the DatasetClient's
                // underlying HttpClient might have timed out.
                attempt.AddError(ex);
                return Result.Failure;
            }
            catch (Exception ex)
            {
                attempt.AddError(ex, "Error getting dataset refresh status");
                return Result.Failure;
            }
        }
    }
}
