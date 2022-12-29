using Biflow.DataAccess;
using Biflow.DataAccess.Models;
using Biflow.Executor.Core.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Polly;
using System.Text;

namespace Biflow.Executor.Core.StepExecutor;

internal class PipelineStepExecutor : StepExecutorBase
{
    private readonly ILogger<PipelineStepExecutor> _logger;
    private readonly IExecutionConfiguration _executionConfiguration;
    private readonly ITokenService _tokenService;
    private readonly IDbContextFactory<BiflowContext> _dbContextFactory;

    private PipelineStepExecution Step { get; }
    
    private PipelineClient PipelineClient => Step.PipelineClient;

    private const int MaxRefreshRetries = 3;

    private StringBuilder Warning { get; } = new StringBuilder();

    public PipelineStepExecutor(
        ILogger<PipelineStepExecutor> logger,
        IExecutionConfiguration executionConfiguration,
        ITokenService tokenService,
        IDbContextFactory<BiflowContext> dbContextFactory,
        PipelineStepExecution step)
        : base(logger, dbContextFactory, step)
    {
        _logger = logger;
        _executionConfiguration = executionConfiguration;
        _tokenService = tokenService;
        _dbContextFactory = dbContextFactory;
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
                .ToDictionary(key => key.ParameterName, value => value.ParameterValue);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{ExecutionId} {Step} Error retrieving pipeline parameters", Step.ExecutionId, Step);
            return Result.Failure($"Error reading pipeline parameters:\n{ex.Message}", Warning.ToString());
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
            return Result.Failure($"Error starting pipeline run:\n{ex.Message}", Warning.ToString());
        }

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
            Warning.AppendLine($"Error updating pipeline run id {runId}\n{ex.Message}");
        }

        using var timeoutCts = Step.TimeoutMinutes > 0
                    ? new CancellationTokenSource(TimeSpan.FromMinutes(Step.TimeoutMinutes))
                    : new CancellationTokenSource();
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

        string status;
        string message;
        while (true)
        {
            try
            {
                (status, message) = await GetPipelineRunWithRetriesAsync(runId, linkedCts.Token);
                if (status == "InProgress" || status == "Queued")
                {
                    await Task.Delay(_executionConfiguration.PollingIntervalMs, linkedCts.Token);
                }
                else
                {
                    break;
                }
            }
            catch (OperationCanceledException)
            {
                await CancelAsync(runId);
                if (timeoutCts.IsCancellationRequested)
                {
                    _logger.LogWarning("{ExecutionId} {Step} Step execution timed out", Step.ExecutionId, Step);
                    return Result.Failure("Step execution timed out", Warning.ToString()); // Report failure => allow possible retries
                }
                throw; // Step was canceled => pass the exception => no retries
            }
            catch (Exception ex)
            {
                return Result.Failure($"Error getting pipeline run status\n{ex.Message}", Warning.ToString());
            }
        }

        if (status == "Succeeded")
        {
            return Result.Success(null, Warning.ToString());
        }
        else
        {
            return Result.Failure(message, Warning.ToString());
        }
    }

    private async Task<(string Status, string Message)> GetPipelineRunWithRetriesAsync(string runId, CancellationToken cancellationToken)
    {
        var policy = Policy
            .Handle<Exception>()
            .WaitAndRetryAsync(
            retryCount: MaxRefreshRetries,
            sleepDurationProvider: retryCount => TimeSpan.FromMilliseconds(_executionConfiguration.PollingIntervalMs),
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
            Warning.AppendLine($"Error stopping pipeline run {runId}\n{ex.Message}");
        }
    }
}
