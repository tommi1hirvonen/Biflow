using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;

namespace Biflow.Executor.Core.StepExecutor;

internal class PipelineStepExecutor(
    IServiceProvider serviceProvider,
    PipelineStepExecution step,
    PipelineStepExecutionAttempt attempt) : IStepExecutor
{
    private readonly ILogger<PipelineStepExecutor> _logger = serviceProvider
        .GetRequiredService<ILogger<PipelineStepExecutor>>();
    private readonly IDbContextFactory<ExecutorDbContext> _dbContextFactory = serviceProvider
        .GetRequiredService<IDbContextFactory<ExecutorDbContext>>();
    private readonly IPipelineClient _client = step
        .GetClient()
        ?.CreatePipelineClient(serviceProvider.GetRequiredService<ITokenService>())
        ?? throw new ArgumentNullException(message: "Pipeline client was null", innerException: null);
    private readonly int _pollingIntervalMs = serviceProvider
        .GetRequiredService<IOptionsMonitor<ExecutionOptions>>()
        .CurrentValue
        .PollingIntervalMs;

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
            _logger.LogError(ex, "{ExecutionId} {Step} Error retrieving pipeline parameters", step.ExecutionId, step);
            attempt.AddError(ex, "Error reading pipeline parameters");
            return Result.Failure;
        }

        string runId;
        try
        {
            runId = await _client.StartPipelineRunAsync(step.PipelineName, parameters, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "{ExecutionId} {Step} Error creating pipeline run for Pipeline Client id {PipelineClientId} and pipeline {PipelineName}",
                step.ExecutionId, step, step.PipelineClientId, step.PipelineName);
            attempt.AddError(ex, "Error starting pipeline run");
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
            attempt.PipelineRunId = runId;
            await dbContext.Set<PipelineStepExecutionAttempt>()
                .Where(x => x.ExecutionId == attempt.ExecutionId &&
                            x.StepId == attempt.StepId &&
                            x.RetryAttemptIndex == attempt.RetryAttemptIndex)
                .ExecuteUpdateAsync(x => x
                    .SetProperty(p => p.PipelineRunId, attempt.PipelineRunId), CancellationToken.None);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "{ExecutionId} {Step} Error updating pipeline run id", step.ExecutionId, step);
            attempt.AddWarning(ex, $"Error updating pipeline run id {runId}");
        }

        string status, message;
        try
        {
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);
            while (true)
            {
                (status, message) = await GetPipelineRunWithRetriesAsync(runId, linkedCts.Token);
                if (status is "InProgress" or "Queued")
                {
                    await Task.Delay(_pollingIntervalMs, linkedCts.Token);
                }
                else
                {
                    break;
                }
            }
        }
        catch (OperationCanceledException ex)
        {
            await CancelAsync(runId);
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
            attempt.AddError(ex, "Error getting pipeline run status");
            return Result.Failure;
        }

        if (status == "Succeeded")
        {
            attempt.AddOutput(message);
            return Result.Success;
        }

        attempt.AddError(message);
        return Result.Failure;
    }

    private async Task<(string Status, string Message)> GetPipelineRunWithRetriesAsync(string runId,
        CancellationToken cancellationToken)
    {
        var policy = Policy.Handle<Exception>().WaitAndRetryAsync(
            retryCount: MaxRefreshRetries,
            sleepDurationProvider: retryCount => TimeSpan.FromMilliseconds(_pollingIntervalMs * retryCount),
            onRetry: (ex, _) => _logger.LogWarning(ex,
                "{ExecutionId} {Step} Error getting pipeline run status for run id {runId}",
                step.ExecutionId, step, runId));
        return await policy.ExecuteAsync(cancellation =>
            _client.GetPipelineRunAsync(runId, cancellation), cancellationToken);
    }

    private async Task CancelAsync(string runId)
    {
        _logger.LogInformation("{ExecutionId} {Step} Stopping pipeline run id {PipelineRunId}",
            step.ExecutionId, step, runId);
        try
        {
            await _client.CancelPipelineRunAsync(runId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{ExecutionId} {Step} Error stopping pipeline run {runId}",
                step.ExecutionId, step, runId);
            attempt.AddWarning(ex, $"Error stopping pipeline run {runId}");
        }
    }
}
