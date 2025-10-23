using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.PowerBI.Api.Models;
using Polly;

namespace Biflow.Executor.Core.StepExecutor;

[UsedImplicitly]
internal class DataflowStepExecutor(
    ILogger<DataflowStepExecutor> logger,
    IOptionsMonitor<ExecutionOptions> options,
    ITokenService tokenService,
    IHttpClientFactory httpClientFactory,
    DataflowStepExecution step,
    DataflowStepExecutionAttempt attempt) : IStepExecutor
{
    private readonly int _pollingIntervalMs = options.CurrentValue.PollingIntervalMs;
    private readonly DataflowClient _client =
        step.GetAzureCredential()?.CreateDataflowClient(tokenService, httpClientFactory)
        ?? throw new ArgumentNullException(message: "Azure credential was null", innerException: null);
    
    private const int MaxRefreshRetries = 3;

    public async Task<Result> ExecuteAsync(OrchestrationContext context, ExtendedCancellationTokenSource cts)
    {
        var cancellationToken = cts.Token;
        cancellationToken.ThrowIfCancellationRequested();

        // Start dataflow refresh.
        try
        {
            await _client.RefreshDataflowAsync(step.WorkspaceId, step.DataflowId, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error starting dataflow refresh");
            attempt.AddError(ex, "Error starting dataflow refresh operation");
            return Result.Failure;
        }
        
        // Initialize timeout cancellation token source already here
        // so that we can start the countdown immediately after the refresh was started.
        using var timeoutCts = step.TimeoutMinutes > 0
            ? new CancellationTokenSource(TimeSpan.FromMinutes(step.TimeoutMinutes))
            : new CancellationTokenSource();

        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);
        
        DataflowTransaction? transaction = null;
        try
        {
            // Wait for 5 seconds before first attempting to get the dataflow transaction status.
            await Task.Delay(5000, linkedCts.Token);
            
            while (true)
            {
                (var status, transaction) = await GetDataflowTransactionStatusWithRetriesAsync(linkedCts.Token);
                switch (status)
                {
                    case DataflowRefreshStatus.Success:
                        attempt.AddOutput($"Dataflow transaction id: {transaction.Id}");
                        return Result.Success;
                    case DataflowRefreshStatus.Failed or DataflowRefreshStatus.Cancelled:
                        attempt.AddOutput($"Dataflow transaction id: {transaction.Id}");
                        attempt.AddError($"Dataflow transaction reported status: {status}");
                        return Result.Failure;
                    default:
                        await Task.Delay(_pollingIntervalMs, linkedCts.Token);
                        break;
                }
            }
        }
        catch (OperationCanceledException ex)
        {
            if (transaction is not null)
            {
                attempt.AddOutput($"Dataflow transaction id: {transaction.Id}");
                await CancelAsync(transaction);
            }
            if (timeoutCts.IsCancellationRequested)
            {
                attempt.AddError(ex, "Step execution timed out");
                return Result.Failure;
            }
            // If the linked token was canceled, report result as 'Cancel'.
            if (linkedCts.Token.IsCancellationRequested)
            {
                attempt.AddWarning(ex);
                return Result.Cancel;
            }
            // If not, report error. This means the step was not canceled, but instead the DataflowClient's
            // underlying HttpClient might have timed out.
            attempt.AddError(ex);
            return Result.Failure;
        }
        catch (Exception ex)
        {
            if (transaction is not null)
            {
                attempt.AddOutput($"Dataflow transaction id: {transaction.Id}");
            }
            attempt.AddError(ex, "Error getting dataflow refresh status");
            return Result.Failure;
        }
    }

    private async Task<(DataflowRefreshStatus Status, DataflowTransaction Transaction)>
        GetDataflowTransactionStatusWithRetriesAsync(CancellationToken cancellationToken)
    {
        var policy = Policy
            .Handle<Exception>()
            .WaitAndRetryAsync(
                retryCount: MaxRefreshRetries,
                sleepDurationProvider: _ => TimeSpan.FromMilliseconds(_pollingIntervalMs),
                onRetry: (ex, _) =>
                    logger.LogWarning(ex, "{ExecutionId} {Step} Error getting dataflow transaction status",
                        step.ExecutionId, step));

        return await policy.ExecuteAsync(cancellation =>
            _client.GetDataflowTransactionStatusAsync(step.WorkspaceId, step.DataflowId, cancellation),
            cancellationToken);
    }
    
    private async Task CancelAsync(DataflowTransaction transaction)
    {
        logger.LogInformation("{ExecutionId} {Step} Stopping dataflow transaction id {transactionId}",
            step.ExecutionId, step, transaction.Id);
        try
        {
            await _client.CancelDataflowRefreshAsync(step.WorkspaceId, transaction);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "{ExecutionId} {Step} Error stopping dataflow transaction id {transactionId}",
                step.ExecutionId, step, transaction.Id);
            attempt.AddWarning(ex, $"Error stopping dataflow transaction id {transaction.Id}");
        }
    }
    
    public void Dispose()
    {
    }
}