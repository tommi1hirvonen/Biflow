using System.Text.Json;
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
    private readonly int _pollingIntervalMs = serviceProvider
        .GetRequiredService<IOptionsMonitor<ExecutionOptions>>()
        .CurrentValue
        .PollingIntervalMs;
    private readonly FabricWorkspaceClient _client = step
        .GetAzureCredential()
        ?.CreateFabricWorkspaceClient(serviceProvider.GetRequiredService<ITokenService>())
        ?? throw new ArgumentNullException(message: "Azure credential was null", innerException: null);
    private readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = true };
    
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
        
        Guid instanceId;
        try
        {
            instanceId = await _client.StartOnDemandItemJobAsync(
                step.WorkspaceId, step.ItemId, step.ItemType, parameters, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "{ExecutionId} {Step} Error creating item job instance for workspace id {WorkspaceId} and item {ItemId}",
                step.ExecutionId,
                step,
                step.WorkspaceId,
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
                
                instance = await GetItemJobInstanceWithRetriesAsync(instanceId, linkedCts.Token);
                
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
            await CancelAsync(instanceId);
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
    
    private async Task<ItemJobInstance> GetItemJobInstanceWithRetriesAsync(Guid instanceId,
        CancellationToken cancellationToken)
    {
        var policy = Policy.Handle<Exception>().WaitAndRetryAsync(
            retryCount: MaxRefreshRetries,
            sleepDurationProvider: retryCount => TimeSpan.FromMilliseconds(_pollingIntervalMs * retryCount),
            onRetry: (ex, _) => _logger.LogWarning(ex,
                "{ExecutionId} {Step} Error getting item job instance for instance id {instanceId}",
                step.ExecutionId, step, instanceId));
        return await policy.ExecuteAsync(cancellation =>
            _client.GetItemJobInstanceAsync(step.WorkspaceId, step.ItemId, instanceId, cancellation), cancellationToken);
    }
    
    private async Task CancelAsync(Guid instanceId)
    {
        _logger.LogInformation("{ExecutionId} {Step} Stopping item job instance id {instanceId}",
            step.ExecutionId, step, instanceId);
        try
        {
            await _client.CancelItemJobInstanceAsync(step.WorkspaceId, step.ItemId, instanceId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{ExecutionId} {Step} Error stopping item job instance {instanceId}",
                step.ExecutionId, step, instanceId);
            attempt.AddWarning(ex, $"Error stopping item job instance {instanceId}");
        }
    }
}