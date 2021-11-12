using EtlManager.DataAccess;
using EtlManager.DataAccess.Models;
using Microsoft.Azure.Management.DataFactory.Models;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace EtlManager.Executor;

class PipelineStepExecutor : StepExecutorBase
{
    private readonly IExecutionConfiguration _executionConfiguration;
    private readonly ITokenService _tokenService;
    private readonly IDbContextFactory<EtlManagerContext> _dbContextFactory;

    private PipelineStepExecution Step { get; init; }
    private DataFactory DataFactory => Step.DataFactory;

    private const int MaxRefreshRetries = 3;

    public PipelineStepExecutor(
        IExecutionConfiguration executionConfiguration,
        ITokenService tokenService,
        IDbContextFactory<EtlManagerContext> dbContextFactory,
        PipelineStepExecution step)
        : base(dbContextFactory, step)
    {
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
            Log.Error(ex, "{ExecutionId} {Step} Error retrieving pipeline parameters", Step.ExecutionId, Step);
            return Result.Failure("Error reading pipeline parameters: " + ex.Message);
        }

        string runId;
        try
        {
            runId = await DataFactory.StartPipelineRunAsync(_tokenService, Step.PipelineName, parameters, cancellationToken);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "{ExecutionId} {Step} Error creating pipeline run for Data Factory id {DataFactoryId} and pipeline {PipelineName}",
                Step.ExecutionId, Step, Step.DataFactoryId, Step.PipelineName);
            return Result.Failure($"Error starting pipeline run:\n{ex.Message}");
        }

        using var timeoutCts = Step.TimeoutMinutes > 0
                    ? new CancellationTokenSource(TimeSpan.FromMinutes(Step.TimeoutMinutes))
                    : new CancellationTokenSource();
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

        try
        {
            using var context = _dbContextFactory.CreateDbContext();
            var attempt = Step.StepExecutionAttempts.FirstOrDefault(e => e.RetryAttemptIndex == RetryAttemptCounter);
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
            Log.Warning(ex, "{ExecutionId} {Step} Error updating pipeline run id", Step.ExecutionId, Step);
        }

        PipelineRun pipelineRun;
        while (true)
        {
            try
            {
                pipelineRun = await TryGetPipelineRunAsync(runId, linkedCts.Token);
                if (pipelineRun.Status == "InProgress" || pipelineRun.Status == "Queued")
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
                    Log.Warning("{ExecutionId} {Step} Step execution timed out", Step.ExecutionId, Step);
                    return Result.Failure("Step execution timed out"); // Report failure => allow possible retries
                }
                throw; // Step was canceled => pass the exception => no retries
            }
        }

        if (pipelineRun.Status == "Succeeded")
        {
            return Result.Success();
        }
        else
        {
            return Result.Failure(pipelineRun.Message);
        }
    }

    private async Task<PipelineRun> TryGetPipelineRunAsync(string runId, CancellationToken cancellationToken)
    {
        int refreshRetries = 0;
        while (refreshRetries < MaxRefreshRetries)
        {
            try
            {
                return await DataFactory!.GetPipelineRunAsync(_tokenService, runId, CancellationToken.None);
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "{ExecutionId} {Step} Error getting pipeline run status for run id {runId}", Step.ExecutionId, Step, runId);
                refreshRetries++;
                await Task.Delay(_executionConfiguration.PollingIntervalMs, cancellationToken);
            }
        }
        throw new TimeoutException("The maximum number of pipeline run status refresh attempts was reached.");
    }

    private async Task CancelAsync(string runId)
    {
        Log.Information("{ExecutionId} {Step} Stopping pipeline run id {PipelineRunId}", Step.ExecutionId, Step, runId);
        try
        {
            await DataFactory!.CancelPipelineRunAsync(_tokenService, runId);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "{ExecutionId} {Step} Error stopping pipeline run {runId}", Step.ExecutionId, Step, runId);
        }
    }
}
