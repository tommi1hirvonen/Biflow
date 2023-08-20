using Biflow.DataAccess;
using Biflow.DataAccess.Models;
using Biflow.Executor.Core.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;

namespace Biflow.Executor.Core.StepExecutor;

internal class PipelineStepExecutor : StepExecutorBase
{
    private readonly ILogger<PipelineStepExecutor> _logger;
    private readonly ITokenService _tokenService;
    private readonly IDbContextFactory<ExecutorDbContext> _dbContextFactory;
    private readonly int _pollingIntervalMs;

    private PipelineStepExecution Step { get; }
    
    private PipelineClient PipelineClient => Step.PipelineClient;

    private const int MaxRefreshRetries = 3;

    public PipelineStepExecutor(
        ILogger<PipelineStepExecutor> logger,
        IOptionsMonitor<ExecutionOptions> options,
        ITokenService tokenService,
        IDbContextFactory<ExecutorDbContext> dbContextFactory,
        PipelineStepExecution step)
        : base(logger, dbContextFactory, step)
    {
        _logger = logger;
        _tokenService = tokenService;
        _dbContextFactory = dbContextFactory;
        _pollingIntervalMs = options.CurrentValue.PollingIntervalMs;
        Step = step;
    }

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
            return new Failure(ex, "Error reading pipeline parameters");
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
            return new Failure(ex, "Error starting pipeline run");
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
            return timeoutCts.IsCancellationRequested
                ? new Failure(ex, "Step execution timed out")
                : new Cancel(ex);
        }
        catch (Exception ex)
        {
            return new Failure(ex, "Error getting pipeline run status");
        }

        if (status == "Succeeded")
        {
            AddOutput(message);
            return new Success();
        }
        else
        {
            return new Failure(message);
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
