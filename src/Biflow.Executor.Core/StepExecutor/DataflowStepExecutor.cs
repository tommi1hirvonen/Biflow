using Biflow.Executor.Core.Cache;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.PowerBI.Api.Models;
using Polly;

namespace Biflow.Executor.Core.StepExecutor;

internal class DataflowStepExecutor : IStepExecutor
{
    private readonly ILogger<DataflowStepExecutor> _logger;
    private readonly int _pollingIntervalMs;
    private readonly DataflowCache _cache;
    private readonly FabricWorkspace _workspace;
    private readonly DataflowClient _client;
    private readonly DataflowStepExecution _step;
    private readonly DataflowStepExecutionAttempt _attempt;

    public DataflowStepExecutor(IServiceProvider serviceProvider,
        DataflowStepExecution step,
        DataflowStepExecutionAttempt attempt)
    {
        _step = step;
        _attempt = attempt;
        _logger = serviceProvider.GetRequiredService<ILogger<DataflowStepExecutor>>();
        _pollingIntervalMs = serviceProvider.GetRequiredService<IOptionsMonitor<ExecutionOptions>>()
            .CurrentValue
            .PollingIntervalMs;
        _cache = serviceProvider.GetRequiredService<DataflowCache>();
        _workspace = step.GetFabricWorkspace()
            ?? throw new ArgumentNullException(message: "Fabric workspace was null", innerException: null);
        var credential = _workspace.AzureCredential
            ?? throw new ArgumentNullException(message: "Azure credential was null", innerException: null);
        _client = serviceProvider.GetRequiredService<DataflowClientFactory>().Create(credential);
    }

    private const int MaxGetDataflowIdRetries = 3;
    private const int MaxRefreshRetries = 3;

    public async Task<Result> ExecuteAsync(OrchestrationContext context, CancellationContext cancellationContext)
    {
        var cancellationToken = cancellationContext.CancellationToken;
        cancellationToken.ThrowIfCancellationRequested();
        
        // Get the dataflow id based on the dataflow name. Because of environment transfers of metadata,
        // the dataflow id is not the preferred or primary way to identify dataflows.
        // Dataflow ids differ for the "same" dataflow in different Power BI environments (or workspaces, in practice).
        Guid dataflowId;
        try
        {
            dataflowId = await GetDataflowIdWithRetriesAsync(cancellationToken);
        }
        catch (ArgumentNullException ex)
        {
            // GetDataflowIdWithRetriesAsync() throws ArgumentNullException if the dataflow id was not found.
            _attempt.AddWarning(ex, "No dataflow id was found for the specified name. Using stored dataflow id instead.");
            dataflowId = Guid.Parse(_step.DataflowId);
        }
        catch (Exception ex)
        {
            _attempt.AddError(ex, "Error getting dataflow id");
            return Result.Failure;
        }

        // Start dataflow refresh.
        try
        {
            await _client.RefreshDataflowAsync(_workspace.WorkspaceId, dataflowId, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting dataflow refresh");
            _attempt.AddError(ex, "Error starting dataflow refresh operation");
            return Result.Failure;
        }
        
        // Initialize timeout cancellation token source already here
        // so that we can start the countdown immediately after the refresh was started.
        using var timeoutCts = _step.TimeoutMinutes > 0
            ? new CancellationTokenSource(TimeSpan.FromMinutes(_step.TimeoutMinutes))
            : new CancellationTokenSource();

        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);
        
        DataflowTransaction? transaction = null;
        try
        {
            // Wait for 5 seconds before first attempting to get the dataflow transaction status.
            await Task.Delay(5000, linkedCts.Token);
            
            while (true)
            {
                (var status, transaction) = await GetDataflowTransactionStatusWithRetriesAsync(dataflowId,
                    linkedCts.Token);
                switch (status)
                {
                    case DataflowRefreshStatus.Success:
                        _attempt.AddOutput($"Dataflow transaction id: {transaction.Id}");
                        return Result.Success;
                    case DataflowRefreshStatus.Failed or DataflowRefreshStatus.Cancelled:
                        _attempt.AddOutput($"Dataflow transaction id: {transaction.Id}");
                        _attempt.AddError($"Dataflow transaction reported status: {status}");
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
                _attempt.AddOutput($"Dataflow transaction id: {transaction.Id}");
                await CancelAsync(transaction);
            }
            if (timeoutCts.IsCancellationRequested)
            {
                _attempt.AddError(ex, "Step execution timed out");
                return Result.Failure;
            }
            // If the linked token was canceled, report result as 'Cancel'.
            if (linkedCts.Token.IsCancellationRequested)
            {
                _attempt.AddWarning(ex);
                return Result.Cancel;
            }
            // If not, report error. This means the step was not canceled, but instead the DataflowClient's
            // underlying HttpClient might have timed out.
            _attempt.AddError(ex);
            return Result.Failure;
        }
        catch (Exception ex)
        {
            if (transaction is not null)
            {
                _attempt.AddOutput($"Dataflow transaction id: {transaction.Id}");
            }
            _attempt.AddError(ex, "Error getting dataflow refresh status");
            return Result.Failure;
        }
    }
    
    private async Task<Guid> GetDataflowIdWithRetriesAsync(CancellationToken cancellationToken)
    {
        var policy = Policy.Handle<Exception>().WaitAndRetryAsync(
            retryCount: MaxGetDataflowIdRetries,
            sleepDurationProvider: retryCount => TimeSpan.FromMilliseconds(_pollingIntervalMs * retryCount),
            onRetry: (ex, _) => _logger.LogWarning(ex,
                "{ExecutionId} {Step} Error getting dataflow id for name {itemName}",
                _step.ExecutionId, _step, _step.DataflowName));
        var dataflowId = await policy.ExecuteAsync(cancellation =>
            _cache.GetDataflowIdAsync(_client, _step.ExecutionId, _workspace.WorkspaceId, _step.DataflowName,
                cancellation), cancellationToken)
            ?? throw new ArgumentNullException(message: $"Dataflow id not found for name '{_step.DataflowName}'",
                innerException: null);
        return dataflowId;
    }

    private async Task<(DataflowRefreshStatus Status, DataflowTransaction Transaction)>
        GetDataflowTransactionStatusWithRetriesAsync(Guid dataflowId, CancellationToken cancellationToken)
    {
        var policy = Policy.Handle<Exception>().WaitAndRetryAsync(
            retryCount: MaxRefreshRetries,
            sleepDurationProvider: retryCount => TimeSpan.FromMilliseconds(_pollingIntervalMs * retryCount),
            onRetry: (ex, _) => _logger.LogWarning(ex,
                "{ExecutionId} {Step} Error getting dataflow transaction status", _step.ExecutionId, _step));
        return await policy.ExecuteAsync(cancellation =>
            _client.GetDataflowTransactionStatusAsync(_workspace.WorkspaceId, dataflowId, cancellation),
            cancellationToken);
    }
    
    private async Task CancelAsync(DataflowTransaction transaction)
    {
        _logger.LogInformation("{ExecutionId} {Step} Stopping dataflow transaction id {transactionId}",
            _step.ExecutionId, _step, transaction.Id);
        try
        {
            await _client.CancelDataflowRefreshAsync(_workspace.WorkspaceId, transaction);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{ExecutionId} {Step} Error stopping dataflow transaction id {transactionId}",
                _step.ExecutionId, _step, transaction.Id);
            _attempt.AddWarning(ex, $"Error stopping dataflow transaction id {transaction.Id}");
        }
    }
}