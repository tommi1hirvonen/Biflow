using Biflow.Executor.Core.Common;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;

namespace Biflow.Executor.Core.StepExecutor;

internal class PipelineStepExecutor(
    ILogger<PipelineStepExecutor> logger,
    IOptionsMonitor<ExecutionOptions> options,
    IDbContextFactory<ExecutorDbContext> dbContextFactory,
    ITokenService tokenService)
    : StepExecutor<PipelineStepExecution, PipelineStepExecutionAttempt>(logger, dbContextFactory)
{
    private readonly ILogger<PipelineStepExecutor> _logger = logger;
    private readonly IDbContextFactory<ExecutorDbContext> _dbContextFactory = dbContextFactory;
    private readonly int _pollingIntervalMs = options.CurrentValue.PollingIntervalMs;
    private readonly ITokenService _tokenService = tokenService;

    private const int MaxRefreshRetries = 3;

    protected override async Task<Result> ExecuteAsync(
        PipelineStepExecution step,
        PipelineStepExecutionAttempt attempt,
        ExtendedCancellationTokenSource cancellationTokenSource)
    {
        var cancellationToken = cancellationTokenSource.Token;
        cancellationToken.ThrowIfCancellationRequested();

        var client = step.GetClient()?.CreatePipelineClient(_tokenService);
        ArgumentNullException.ThrowIfNull(client);

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
            runId = await client.StartPipelineRunAsync(step.PipelineName, parameters, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{ExecutionId} {Step} Error creating pipeline run for Pipeline Client id {PipelineClientId} and pipeline {PipelineName}",
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
            using var context = _dbContextFactory.CreateDbContext();
            attempt.PipelineRunId = runId;
            context.Attach(attempt);
            context.Entry(attempt).Property(e => e.PipelineRunId).IsModified = true;
            await context.SaveChangesAsync(CancellationToken.None);
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
                (status, message) = await GetPipelineRunWithRetriesAsync(client, step, runId, linkedCts.Token);
                if (status == "InProgress" || status == "Queued")
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
            await CancelAsync(client, step, attempt, runId);
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
        else
        {
            attempt.AddError(message);
            return Result.Failure;
        }
    }

    private async Task<(string Status, string Message)> GetPipelineRunWithRetriesAsync(
        IPipelineClient client,
        PipelineStepExecution step,
        string runId,
        CancellationToken cancellationToken)
    {
        var policy = Policy
            .Handle<Exception>()
            .WaitAndRetryAsync(
            retryCount: MaxRefreshRetries,
            sleepDurationProvider: retryCount => TimeSpan.FromMilliseconds(_pollingIntervalMs),
            onRetry: (ex, waitDuration) =>
                _logger.LogWarning(ex, "{ExecutionId} {Step} Error getting pipeline run status for run id {runId}", step.ExecutionId, step, runId));

        return await policy.ExecuteAsync((cancellationToken) =>
            client.GetPipelineRunAsync(runId, cancellationToken), cancellationToken);
    }

    private async Task CancelAsync(
        IPipelineClient client,
        PipelineStepExecution step,
        PipelineStepExecutionAttempt attempt,
        string runId)
    {
        _logger.LogInformation("{ExecutionId} {Step} Stopping pipeline run id {PipelineRunId}", step.ExecutionId, step, runId);
        try
        {
            await client.CancelPipelineRunAsync(runId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{ExecutionId} {Step} Error stopping pipeline run {runId}", step.ExecutionId, step, runId);
            attempt.AddWarning(ex, $"Error stopping pipeline run {runId}");
        }
    }
}
