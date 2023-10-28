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
    PipelineStepExecution step) : StepExecutorBase(logger, dbContextFactory, step)
{
    private readonly ILogger<PipelineStepExecutor> _logger = logger;
    private readonly ITokenService _tokenService = tokenService;
    private readonly IDbContextFactory<ExecutorDbContext> _dbContextFactory = dbContextFactory;
    private readonly int _pollingIntervalMs = options.CurrentValue.PollingIntervalMs;

    private PipelineStepExecution Step { get; } = step;

    private PipelineClient PipelineClient => Step.PipelineClient;

    private const int MaxRefreshRetries = 3;

    protected override async Task<Result> ExecuteAsync(ExtendedCancellationTokenSource cancellationTokenSource)
    {
        var cancellationToken = cancellationTokenSource.Token;
        cancellationToken.ThrowIfCancellationRequested();

        // Get possible parameters.
        IDictionary<string, object> parameters;
        try
        {
            parameters = Step.StepExecutionParameters
                .Where(p => p.ParameterValue is not null)
                .ToDictionary(key => key.ParameterName, value => value.ParameterValue!);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{ExecutionId} {Step} Error retrieving pipeline parameters", Step.ExecutionId, Step);
            AddError(ex, "Error reading pipeline parameters");
            return Result.Failure;
        }

        string runId;
        try
        {
            runId = await PipelineClient.StartPipelineRunAsync(_tokenService, Step.PipelineName, parameters, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{ExecutionId} {Step} Error creating pipeline run for Pipeline Client id {PipelineClientId} and pipeline {PipelineName}",
                Step.ExecutionId, Step, Step.PipelineClientId, Step.PipelineName);
            AddError(ex, "Error starting pipeline run");
            return Result.Failure;
        }

        // Initialize timeout cancellation token source already here
        // so that we can start the countdown immediately after the pipeline was started.
        using var timeoutCts = Step.TimeoutMinutes > 0
                    ? new CancellationTokenSource(TimeSpan.FromMinutes(Step.TimeoutMinutes))
                    : new CancellationTokenSource();

        try
        {
            using var context = _dbContextFactory.CreateDbContext();
            var attempt = Step.StepExecutionAttempts.MaxBy(e => e.RetryAttemptIndex);
            if (attempt is not null && attempt is PipelineStepExecutionAttempt pipeline)
            {
                pipeline.PipelineRunId = runId;
                context.Attach(pipeline);
                context.Entry(pipeline).Property(e => e.PipelineRunId).IsModified = true;
                await context.SaveChangesAsync(CancellationToken.None);
            }
            else
            {
                throw new InvalidOperationException("Could not find step execution attempt to update pipeline run id");
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "{ExecutionId} {Step} Error updating pipeline run id", Step.ExecutionId, Step);
            AddWarning(ex, $"Error updating pipeline run id {runId}");
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
            await CancelAsync(runId);
            if (timeoutCts.IsCancellationRequested)
            {
                AddError(ex, "Step execution timed out");
                return Result.Failure;
            }
            AddWarning(ex);
            return Result.Cancel;
        }
        catch (Exception ex)
        {
            AddError(ex, "Error getting pipeline run status");
            return Result.Failure;
        }

        if (status == "Succeeded")
        {
            AddOutput(message);
            return Result.Success;
        }
        else
        {
            AddError(message);
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
                _logger.LogWarning(ex, "{ExecutionId} {Step} Error getting pipeline run status for run id {runId}", Step.ExecutionId, Step, runId));

        return await policy.ExecuteAsync((cancellationToken) =>
            PipelineClient.GetPipelineRunAsync(_tokenService, runId, cancellationToken), cancellationToken);
    }

    private async Task CancelAsync(string runId)
    {
        _logger.LogInformation("{ExecutionId} {Step} Stopping pipeline run id {PipelineRunId}", Step.ExecutionId, Step, runId);
        try
        {
            await PipelineClient.CancelPipelineRunAsync(_tokenService, runId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{ExecutionId} {Step} Error stopping pipeline run {runId}", Step.ExecutionId, Step, runId);
            AddWarning(ex, $"Error stopping pipeline run {runId}");
        }
    }
}
