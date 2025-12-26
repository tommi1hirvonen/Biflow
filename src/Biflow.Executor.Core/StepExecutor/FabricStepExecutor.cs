using System.Text.Json;
using Biflow.Executor.Core.Cache;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Fabric.Api.Core.Models;
using Polly;

namespace Biflow.Executor.Core.StepExecutor;

internal class FabricStepExecutor : IStepExecutor
{
    private readonly ILogger<FabricStepExecutor> _logger;
    private readonly IDbContextFactory<ExecutorDbContext> _dbContextFactory;
    private readonly FabricItemCache _cache;
    private readonly int _pollingIntervalMs;
    private readonly FabricWorkspace _workspace;
    private readonly FabricWorkspaceClient _client;
    private readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = true };
    private readonly FabricStepExecution _step;
    private readonly FabricStepExecutionAttempt _attempt;

    public FabricStepExecutor(IServiceProvider serviceProvider,
        FabricStepExecution step,
        FabricStepExecutionAttempt attempt)
    {
        _step = step;
        _attempt = attempt;
        _logger = serviceProvider.GetRequiredService<ILogger<FabricStepExecutor>>();
        _dbContextFactory = serviceProvider.GetRequiredService<IDbContextFactory<ExecutorDbContext>>();
        _cache = serviceProvider.GetRequiredService<FabricItemCache>();
        _pollingIntervalMs = serviceProvider.GetRequiredService<IOptionsMonitor<ExecutionOptions>>()
            .CurrentValue
            .PollingIntervalMs;
        _workspace = step.GetFabricWorkspace()
            ?? throw new ArgumentNullException(message: "Fabric workspace was null", innerException: null);
        var credential = _workspace.AzureCredential
            ?? throw new ArgumentNullException(message: "Azure credential was null", innerException: null);
        _client = serviceProvider.GetRequiredService<FabricWorkspaceClientFactory>().Create(credential);
    }

    private const int MaxGetItemIdRetries = 3;
    private const int MaxRefreshRetries = 3;
    
    public async Task<Result> ExecuteAsync(OrchestrationContext context, CancellationContext cancellationContext)
    {
        var cancellationToken = cancellationContext.CancellationToken;
        cancellationToken.ThrowIfCancellationRequested();
        
        // Get possible parameters.
        IDictionary<string, object> parameters;
        try
        {
            parameters = _step.StepExecutionParameters
                .Where(p => p.ParameterValue.Value is not null)
                .ToDictionary(key => key.ParameterName, value => value.ParameterValue.Value!);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{ExecutionId} {Step} Error retrieving Fabric item parameters", _step.ExecutionId, _step);
            _attempt.AddError(ex, "Error reading Fabric item parameters");
            return Result.Failure;
        }
        
        // Get the item id based on the item name. Because of environment transfers of metadata, the item id is not
        // the preferred or primary way to identify items.
        // Item ids differ for the "same" item in different Fabric environments (or workspaces, in practice).
        Guid itemId;
        try
        {
            itemId = await GetItemIdWithRetriesAsync(cancellationToken);
        }
        catch (ArgumentNullException ex)
        {
            // GetItemIdWithRetriesAsync() throws ArgumentNullException if the item id was not found.
            _attempt.AddWarning(ex, "No item id was found for the specified name. Using stored item id instead.");
            itemId = _step.ItemId;
        }
        catch (Exception ex)
        {
            _attempt.AddError(ex, "Error getting item id");
            return Result.Failure;
        }
        
        Guid instanceId;
        try
        {
            (var success, instanceId, var responseContent) = await _client.StartOnDemandItemJobAsync(
                _workspace.WorkspaceId, itemId, _step.ItemType, parameters, cancellationToken);
            if (!success)
            {
                _attempt.AddError(new Exception("Failed to start on demand item job"));
                _attempt.AddError(responseContent);
                return Result.Failure;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "{ExecutionId} {Step} Error creating item job instance for workspace id {WorkspaceId} and item {ItemId}",
                _step.ExecutionId,
                _step,
                _workspace.WorkspaceId,
                _step.ItemId);
            _attempt.AddError(ex, "Error starting item job instance");
            return Result.Failure;
        }
        
        // Initialize timeout cancellation token source already here
        // so that we can start the countdown immediately after the pipeline was started.
        using var timeoutCts = _step.TimeoutMinutes > 0
            ? new CancellationTokenSource(TimeSpan.FromMinutes(_step.TimeoutMinutes))
            : new CancellationTokenSource();
        
        try
        {
            await using var dbContext = await _dbContextFactory.CreateDbContextAsync(CancellationToken.None);
            _attempt.JobInstanceId = instanceId;
            await dbContext.Set<FabricStepExecutionAttempt>()
                .Where(x => x.ExecutionId == _attempt.ExecutionId &&
                            x.StepId == _attempt.StepId &&
                            x.RetryAttemptIndex == _attempt.RetryAttemptIndex)
                .ExecuteUpdateAsync(x => x
                    .SetProperty(p => p.JobInstanceId, _attempt.JobInstanceId), CancellationToken.None);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "{ExecutionId} {Step} Error updating item job instance id", _step.ExecutionId, _step);
            _attempt.AddWarning(ex, $"Error updating item job instance id {instanceId}");
        }
        
        ItemJobInstance instance;
        try
        {
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);
            while (true)
            {
                await Task.Delay(_pollingIntervalMs, linkedCts.Token);
                
                instance = await GetItemJobInstanceWithRetriesAsync(itemId, instanceId, linkedCts.Token);
                
                if (instance.Status is null ||
                    instance.Status == Status.NotStarted ||
                    instance.Status == Status.InProgress)
                {
                    continue;
                }

                break;
            }
        }
        catch (OperationCanceledException ex)
        {
            await CancelAsync(itemId, instanceId);
            if (timeoutCts.IsCancellationRequested)
            {
                _attempt.AddError(ex, "Step execution timed out");
                return Result.Failure;
            }
            _attempt.AddWarning(ex);
            return Result.Cancel;
        }
        catch (Exception ex)
        {
            _attempt.AddError(ex, "Error getting item job instance");
            return Result.Failure;
        }

        var json = JsonSerializer.Serialize(instance, _jsonOptions);
        _attempt.AddOutput(json);
        
        if (instance.Status == Status.Completed)
        {
            return Result.Success;
        }

        _attempt.AddError(instance.FailureReason.Message);
        return Result.Failure;
    }

    private async Task<Guid> GetItemIdWithRetriesAsync(CancellationToken cancellationToken)
    {
        var policy = Policy.Handle<Exception>().WaitAndRetryAsync(
            retryCount: MaxGetItemIdRetries,
            sleepDurationProvider: retryCount => TimeSpan.FromMilliseconds(_pollingIntervalMs * retryCount),
            onRetry: (ex, _) => _logger.LogWarning(ex,
                "{ExecutionId} {Step} Error getting item id for name {itemName}",
                _step.ExecutionId, _step, _step.ItemName));
        var itemId = await policy.ExecuteAsync(cancellation =>
            _cache.GetItemIdAsync(
                _client,
                _step.ExecutionId,
                _workspace.WorkspaceId,
                _step.ItemType,
                _step.ItemName,
                cancellation),
            cancellationToken)
            ?? throw new ArgumentNullException(message: $"Item id not found for name '{_step.ItemName}'",
                innerException: null);
        return itemId;
    }
    
    private async Task<ItemJobInstance> GetItemJobInstanceWithRetriesAsync(Guid itemId, Guid instanceId,
        CancellationToken cancellationToken)
    {
        var policy = Policy.Handle<Exception>().WaitAndRetryAsync(
            retryCount: MaxRefreshRetries,
            sleepDurationProvider: retryCount => TimeSpan.FromMilliseconds(_pollingIntervalMs * retryCount),
            onRetry: (ex, _) => _logger.LogWarning(ex,
                "{ExecutionId} {Step} Error getting item job instance for instance id {instanceId}",
                _step.ExecutionId, _step, instanceId));
        return await policy.ExecuteAsync(cancellation =>
            _client.GetItemJobInstanceAsync(_workspace.WorkspaceId, itemId, instanceId, cancellation), cancellationToken);
    }
    
    private async Task CancelAsync(Guid itemId, Guid instanceId)
    {
        _logger.LogInformation("{ExecutionId} {Step} Stopping item job instance id {instanceId}",
            _step.ExecutionId, _step, instanceId);
        try
        {
            await _client.CancelItemJobInstanceAsync(_workspace.WorkspaceId, itemId, instanceId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{ExecutionId} {Step} Error stopping item job instance {instanceId}",
                _step.ExecutionId, _step, instanceId);
            _attempt.AddWarning(ex, $"Error stopping item job instance {instanceId}");
        }
    }
}