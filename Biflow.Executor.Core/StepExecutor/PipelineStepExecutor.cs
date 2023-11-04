using Biflow.DataAccess;
using Biflow.DataAccess.Models;
using Biflow.Executor.Core.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;

namespace Biflow.Executor.Core.StepExecutor;

internal class PipelineStepExecutor(
    ILogger<PipelineStepExecutor> logger,
    IOptionsMonitor<ExecutionOptions> options,
    ITokenService tokenService,
    IDbContextFactory<ExecutorDbContext> dbContextFactory,
    PipelineStepExecution step) : IStepExecutor<PipelineStepExecutionAttempt>
{
    private readonly ILogger<PipelineStepExecutor> _logger = logger;
    private readonly ITokenService _tokenService = tokenService;
    private readonly IDbContextFactory<ExecutorDbContext> _dbContextFactory = dbContextFactory;
    private readonly int _pollingIntervalMs = options.CurrentValue.PollingIntervalMs;
    private readonly PipelineStepExecution _step = step;

    private const int MaxRefreshRetries = 3;

    public PipelineStepExecutionAttempt Clone(PipelineStepExecutionAttempt other, int retryAttemptIndex) =>
        new(other, retryAttemptIndex);

    public async Task<Result> ExecuteAsync(PipelineStepExecutionAttempt attempt, ExtendedCancellationTokenSource cancellationTokenSource)
    {
        var cancellationToken = cancellationTokenSource.Token;
        cancellationToken.ThrowIfCancellationRequested();

        // Get possible parameters.
        IDictionary<string, object> parameters;
        try
        {
            parameters = _step.StepExecutionParameters
                .Where(p => p.ParameterValue is not null)
                .ToDictionary(key => key.ParameterName, value => value.ParameterValue!);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{ExecutionId} {Step} Error retrieving pipeline parameters", _step.ExecutionId, _step);
            attempt.AddError(ex, "Error reading pipeline parameters");
            return Result.Failure;
        }

        string runId;
        try
        {
            runId = await _step.PipelineClient.StartPipelineRunAsync(_tokenService, _step.PipelineName, parameters, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{ExecutionId} {Step} Error creating pipeline run for Pipeline Client id {PipelineClientId} and pipeline {PipelineName}",
                _step.ExecutionId, _step, _step.PipelineClientId, _step.PipelineName);
            attempt.AddError(ex, "Error starting pipeline run");
            return Result.Failure;
        }

        // Initialize timeout cancellation token source already here
        // so that we can start the countdown immediately after the pipeline was started.
        using var timeoutCts = _step.TimeoutMinutes > 0
                    ? new CancellationTokenSource(TimeSpan.FromMinutes(_step.TimeoutMinutes))
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
            _logger.LogWarning(ex, "{ExecutionId} {Step} Error updating pipeline run id", _step.ExecutionId, _step);
            attempt.AddWarning(ex, $"Error updating pipeline run id {runId}");
        }

        string status, message;
        try
        {
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);
            while (true)
            {
                (status, message) = await GetPipelineRunWithRetriesAsync(runId, linkedCts.Token);
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
            await CancelAsync(attempt, runId);
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

    private async Task<(string Status, string Message)> GetPipelineRunWithRetriesAsync(string runId, CancellationToken cancellationToken)
    {
        var policy = Policy
            .Handle<Exception>()
            .WaitAndRetryAsync(
            retryCount: MaxRefreshRetries,
            sleepDurationProvider: retryCount => TimeSpan.FromMilliseconds(_pollingIntervalMs),
            onRetry: (ex, waitDuration) =>
                _logger.LogWarning(ex, "{ExecutionId} {Step} Error getting pipeline run status for run id {runId}", _step.ExecutionId, _step, runId));

        return await policy.ExecuteAsync((cancellationToken) =>
            _step.PipelineClient.GetPipelineRunAsync(_tokenService, runId, cancellationToken), cancellationToken);
    }

    private async Task CancelAsync(PipelineStepExecutionAttempt attempt, string runId)
    {
        _logger.LogInformation("{ExecutionId} {Step} Stopping pipeline run id {PipelineRunId}", _step.ExecutionId, _step, runId);
        try
        {
            await _step.PipelineClient.CancelPipelineRunAsync(_tokenService, runId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{ExecutionId} {Step} Error stopping pipeline run {runId}", _step.ExecutionId, _step, runId);
            attempt.AddWarning(ex, $"Error stopping pipeline run {runId}");
        }
    }
}
