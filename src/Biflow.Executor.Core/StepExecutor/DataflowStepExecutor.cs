using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.PowerBI.Api.Models;
using Polly;

namespace Biflow.Executor.Core.StepExecutor;

internal class DataflowStepExecutor(
    IServiceProvider serviceProvider,
    DataflowStepExecution step,
    DataflowStepExecutionAttempt attempt) : IStepExecutor
{
    private readonly ILogger<DataflowStepExecutor> _logger = serviceProvider
        .GetRequiredService<ILogger<DataflowStepExecutor>>();
    private readonly int _pollingIntervalMs = serviceProvider
        .GetRequiredService<IOptionsMonitor<ExecutionOptions>>()
        .CurrentValue
        .PollingIntervalMs;
    private readonly FabricWorkspace _workspace = step
        .GetFabricWorkspace()
        ?? throw new ArgumentNullException(message: "Fabric workspace was null", innerException: null);
    private readonly DataflowClient _client = step
        .GetFabricWorkspace()
        ?.AzureCredential
        ?.CreateDataflowClient(
            serviceProvider.GetRequiredService<ITokenService>(),
            serviceProvider.GetRequiredService<IHttpClientFactory>())
        ?? throw new ArgumentNullException(message: "Azure credential was null", innerException: null);
    
    private const int MaxRefreshRetries = 3;

    public async Task<Result> ExecuteAsync(OrchestrationContext context, CancellationContext cancellationContext)
    {
        var cancellationToken = cancellationContext.CancellationToken;
        cancellationToken.ThrowIfCancellationRequested();

        // Start dataflow refresh.
        try
        {
            await _client.RefreshDataflowAsync(_workspace.WorkspaceId, step.DataflowId, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting dataflow refresh");
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
        var policy = Policy.Handle<Exception>().WaitAndRetryAsync(
            retryCount: MaxRefreshRetries,
            sleepDurationProvider: retryCount => TimeSpan.FromMilliseconds(_pollingIntervalMs * retryCount),
            onRetry: (ex, _) => _logger.LogWarning(ex,
                "{ExecutionId} {Step} Error getting dataflow transaction status", step.ExecutionId, step));
        return await policy.ExecuteAsync(cancellation =>
            _client.GetDataflowTransactionStatusAsync(_workspace.WorkspaceId, step.DataflowId, cancellation),
            cancellationToken);
    }
    
    private async Task CancelAsync(DataflowTransaction transaction)
    {
        _logger.LogInformation("{ExecutionId} {Step} Stopping dataflow transaction id {transactionId}",
            step.ExecutionId, step, transaction.Id);
        try
        {
            await _client.CancelDataflowRefreshAsync(_workspace.WorkspaceId, transaction);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{ExecutionId} {Step} Error stopping dataflow transaction id {transactionId}",
                step.ExecutionId, step, transaction.Id);
            attempt.AddWarning(ex, $"Error stopping dataflow transaction id {transaction.Id}");
        }
    }
}