using Biflow.DataAccess;
using Biflow.DataAccess.Models;
using Biflow.Executor.Core.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

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
            return Result.Failure($"Error reading pipeline parameters:\n{ex.Message}");
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
            return Result.Failure($"Error starting pipeline run:\n{ex.Message}");
        }

        using var timeoutCts = Step.TimeoutMinutes > 0
                    ? new CancellationTokenSource(TimeSpan.FromMinutes(Step.TimeoutMinutes))
                    : new CancellationTokenSource();
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

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
        }

        string status;
        string message;
        while (true)
        {
            try
            {
                (status, message) = await TryGetPipelineRunAsync(runId, linkedCts.Token);
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
                    return Result.Failure("Step execution timed out"); // Report failure => allow possible retries
                }
                throw; // Step was canceled => pass the exception => no retries
            }
        }

        if (status == "Succeeded")
        {
            return Result.Success();
        }
        else
        {
            return Result.Failure(message);
        }
    }

    private async Task<(string Status, string Message)> TryGetPipelineRunAsync(string runId, CancellationToken cancellationToken)
    {
        int refreshRetries = 0;
        while (refreshRetries < MaxRefreshRetries)
        {
            try
            {
                return await PipelineClient!.GetPipelineRunAsync(_tokenService, runId, CancellationToken.None);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "{ExecutionId} {Step} Error getting pipeline run status for run id {runId}", Step.ExecutionId, Step, runId);
                refreshRetries++;
                await Task.Delay(_executionConfiguration.PollingIntervalMs, cancellationToken);
            }
        }
        throw new TimeoutException("The maximum number of pipeline run status refresh attempts was reached.");
    }

    private async Task CancelAsync(string runId)
    {
        _logger.LogInformation("{ExecutionId} {Step} Stopping pipeline run id {PipelineRunId}", Step.ExecutionId, Step, runId);
        try
        {
            await PipelineClient!.CancelPipelineRunAsync(_tokenService, runId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{ExecutionId} {Step} Error stopping pipeline run {runId}", Step.ExecutionId, Step, runId);
        }
    }
}
