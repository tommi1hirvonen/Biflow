using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Fabric.Api.Core.Models;
using Polly;

namespace Biflow.Executor.Core.StepExecutor;

internal class FabricStepExecutor(
    IServiceProvider serviceProvider,
    FabricStepExecution step,
    FabricStepExecutionAttempt attempt) : IStepExecutor
{
    private readonly ILogger<FabricStepExecutor> _logger = serviceProvider
        .GetRequiredService<ILogger<FabricStepExecutor>>();
    private readonly IDbContextFactory<ExecutorDbContext> _dbContextFactory = serviceProvider
        .GetRequiredService<IDbContextFactory<ExecutorDbContext>>();
    private readonly FabricItemCache _cache = serviceProvider.GetRequiredService<FabricItemCache>();
    private readonly int _pollingIntervalMs = serviceProvider
        .GetRequiredService<IOptionsMonitor<ExecutionOptions>>()
        .CurrentValue
        .PollingIntervalMs;
    private readonly FabricWorkspace _workspace = step
        .GetFabricWorkspace()
        ?? throw new ArgumentNullException(message: "Fabric workspace was null", innerException: null);
    private readonly FabricWorkspaceClient _client = step
        .GetFabricWorkspace()
        ?.AzureCredential
        ?.CreateFabricWorkspaceClient(
            serviceProvider.GetRequiredService<ITokenService>(),
            serviceProvider.GetRequiredService<IHttpClientFactory>())
        ?? throw new ArgumentNullException(message: "Azure credential was null", innerException: null);
    private readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = true };
    
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
            parameters = step.StepExecutionParameters
                .Where(p => p.ParameterValue.Value is not null)
                .ToDictionary(key => key.ParameterName, value => value.ParameterValue.Value!);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{ExecutionId} {Step} Error retrieving Fabric item parameters", step.ExecutionId, step);
            attempt.AddError(ex, "Error reading Fabric item parameters");
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
        catch (ArgumentNullException)
        {
            // GetItemIdWithRetriesAsync() throws ArgumentNullException if the item id was not found.
            attempt.AddWarning("No item id was found for the specified name. Using stored item id instead.");
            itemId = step.ItemId;
        }
        catch (Exception ex)
        {
            attempt.AddError(ex, "Error getting item id");
            return Result.Failure;
        }
        
        Guid instanceId;
        try
        {
            (var success, instanceId, var responseContent) = await _client.StartOnDemandItemJobAsync(
                _workspace.WorkspaceId, itemId, step.ItemType, parameters, cancellationToken);
            if (!success)
            {
                attempt.AddError(new Exception("Failed to start on demand item job"));
                attempt.AddError(responseContent);
                return Result.Failure;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "{ExecutionId} {Step} Error creating item job instance for workspace id {WorkspaceId} and item {ItemId}",
                step.ExecutionId,
                step,
                _workspace.WorkspaceId,
                step.ItemId);
            attempt.AddError(ex, "Error starting item job instance");
            return Result.Failure;
        }
        
        // Initialize timeout cancellation token source already here
        // so that we can start the countdown immediately after the pipeline was started.
        using var timeoutCts = step.TimeoutMinutes > 0
            ? new CancellationTokenSource(TimeSpan.FromMinutes(step.TimeoutMinutes))
            : new CancellationTokenSource();
        
        try
        {
            await using var dbContext = await _dbContextFactory.CreateDbContextAsync(CancellationToken.None);
            attempt.JobInstanceId = instanceId;
            await dbContext.Set<FabricStepExecutionAttempt>()
                .Where(x => x.ExecutionId == attempt.ExecutionId &&
                            x.StepId == attempt.StepId &&
                            x.RetryAttemptIndex == attempt.RetryAttemptIndex)
                .ExecuteUpdateAsync(x => x
                    .SetProperty(p => p.JobInstanceId, attempt.JobInstanceId), CancellationToken.None);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "{ExecutionId} {Step} Error updating item job instance id", step.ExecutionId, step);
            attempt.AddWarning(ex, $"Error updating item job instance id {instanceId}");
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
                attempt.AddError(ex, "Step execution timed out");
                return Result.Failure;
            }
            attempt.AddWarning(ex);
            return Result.Cancel;
        }
        catch (Exception ex)
        {
            attempt.AddError(ex, "Error getting item job instance");
            return Result.Failure;
        }

        var json = JsonSerializer.Serialize(instance, _jsonOptions);
        attempt.AddOutput(json);
        
        if (instance.Status == Status.Completed)
        {
            return Result.Success;
        }

        attempt.AddError(instance.FailureReason.Message);
        return Result.Failure;
    }

    private async Task<Guid> GetItemIdWithRetriesAsync(CancellationToken cancellationToken)
    {
        var policy = Policy.Handle<Exception>().WaitAndRetryAsync(
            retryCount: MaxGetItemIdRetries,
            sleepDurationProvider: retryCount => TimeSpan.FromMilliseconds(_pollingIntervalMs * retryCount),
            onRetry: (ex, _) => _logger.LogWarning(ex,
                "{ExecutionId} {Step} Error getting item id for name {itemName}",
                step.ExecutionId, step, step.ItemName));
        var itemId = await policy.ExecuteAsync(cancellation =>
            _cache.GetItemIdAsync(
                _client,
                step.ExecutionId,
                _workspace.WorkspaceId,
                step.ItemType,
                step.ItemName,
                cancellation),
            cancellationToken)
            ?? throw new ArgumentNullException(message: "Item was found but the id was null", innerException: null);
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
                step.ExecutionId, step, instanceId));
        return await policy.ExecuteAsync(cancellation =>
            _client.GetItemJobInstanceAsync(_workspace.WorkspaceId, itemId, instanceId, cancellation), cancellationToken);
    }
    
    private async Task CancelAsync(Guid itemId, Guid instanceId)
    {
        _logger.LogInformation("{ExecutionId} {Step} Stopping item job instance id {instanceId}",
            step.ExecutionId, step, instanceId);
        try
        {
            await _client.CancelItemJobInstanceAsync(_workspace.WorkspaceId, itemId, instanceId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{ExecutionId} {Step} Error stopping item job instance {instanceId}",
                step.ExecutionId, step, instanceId);
            attempt.AddWarning(ex, $"Error stopping item job instance {instanceId}");
        }
    }
}