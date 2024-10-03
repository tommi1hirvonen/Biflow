using Biflow.Executor.Core.Common;
using Microsoft.Azure.Databricks.Client;
using Microsoft.Azure.Databricks.Client.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;

namespace Biflow.Executor.Core.StepExecutor;

internal class DatabricksStepExecutor(
    ILogger<DatabricksStepExecutor> logger,
    IOptionsMonitor<ExecutionOptions> options,
    IDbContextFactory<ExecutorDbContext> dbContextFactory)
    : StepExecutor<DatabricksStepExecution, DatabricksStepExecutionAttempt>(logger, dbContextFactory)
{
    private readonly ILogger<DatabricksStepExecutor> _logger = logger;
    private readonly IDbContextFactory<ExecutorDbContext> _dbContextFactory = dbContextFactory;
    private readonly int _pollingIntervalMs = options.CurrentValue.PollingIntervalMs;

    private const int MaxRefreshRetries = 3;

    protected override async Task<Result> ExecuteAsync(
        DatabricksStepExecution step,
        DatabricksStepExecutionAttempt attempt,
        ExtendedCancellationTokenSource cancellationTokenSource)
    {
        var cancellationToken = cancellationTokenSource.Token;
        cancellationToken.ThrowIfCancellationRequested();

        // Get possible parameters.
        Dictionary<string, string> parameters;
        try
        {
            parameters = step.StepExecutionParameters
                .Where(p => p.ParameterValue.Value is not null)
                .ToDictionary(key => key.ParameterName, value => value.ParameterValue.Value!.ToString()!);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{ExecutionId} {Step} Error retrieving DbNotebook parameters", step.ExecutionId, step);
            attempt.AddError(ex, "Error reading DbNotebook parameters");
            return Result.Failure;
        }

        using var client = step.GetWorkspace()?.CreateClient()?.Client;
        ArgumentNullException.ThrowIfNull(client);

        // Create run submit settings and the notebook task.

        Task<long> startRunTask;

        if (step.DatabricksStepSettings is DbJobStepSettings dbJob)
        {
            var runParams = new RunParameters { JobParams = parameters };
            startRunTask = client.Jobs.RunNow(dbJob.JobId, runParams, cancellationToken: cancellationToken);
        }
        else
        {
            var settings = new RunSubmitSettings { RunName = step.ExecutionId.ToString() };
            var taskKey = step.StepId.ToString();
            var taskSettings = step.DatabricksStepSettings switch
            {
                DbNotebookStepSettings notebook =>
                    settings.AddTask(
                        taskKey,
                        new NotebookTask
                        {
                            NotebookPath = notebook.NotebookPath,
                            BaseParameters = parameters
                        }),
                DbPythonFileStepSettings python =>
                    settings.AddTask(
                        taskKey,
                        new SparkPythonTask
                        {
                            PythonFile = python.FilePath,
                            Parameters = parameters.OrderBy(kvp => kvp.Key).Select(kvp => kvp.Value).ToList()
                        }),
                DbPipelineStepSettings pipeline =>
                    settings.AddTask(
                        taskKey,
                        new PipelineTask
                        {
                            PipelineId = pipeline.PipelineId,
                            FullRefresh = pipeline.PipelineFullRefresh
                        }),
                _ => throw new ArgumentException($"Unhandled step configuration type {step.DatabricksStepSettings.GetType()}")
            };

            if (step.DatabricksStepSettings is DatabricksClusterStepSettings { ClusterConfiguration: ExistingClusterConfiguration existing })
            {
                taskSettings.WithExistingClusterId(existing.ClusterId);
            }
            else if (step.DatabricksStepSettings is DatabricksClusterStepSettings { ClusterConfiguration: NewClusterConfiguration newCluster })
            {
                var cluster = ClusterAttributes.GetNewClusterConfiguration();
                cluster = newCluster.ClusterMode switch
                {
                    SingleNodeClusterConfiguration single =>
                        cluster.WithClusterMode(ClusterMode.SingleNode),
                    FixedMultiNodeClusterConfiguration fix =>
                        cluster.WithClusterMode(ClusterMode.Standard).WithNumberOfWorkers(fix.NumberOfWorkers),
                    AutoscaleMultiNodeClusterConfiguration auto =>
                        cluster.WithClusterMode(ClusterMode.Standard).WithAutoScale(auto.MinimumWorkers, auto.MaximumWorkers),
                    _ => throw new ArgumentException($"Unhandled new cluster configuration type {newCluster.ClusterMode.GetType()}")
                };
                cluster = cluster
                    .WithNodeType(newCluster.NodeTypeId, newCluster.DriverNodeTypeId)
                    .WithRuntimeVersion(newCluster.RuntimeVersion)
                    .WithRuntimeEngine(newCluster.UsePhoton ? RuntimeEngine.PHOTON : RuntimeEngine.STANDARD);
                taskSettings.WithNewCluster(cluster);
            }

            startRunTask = client.Jobs.RunSubmit(settings, cancellationToken: cancellationToken);
        }

        long runId;
        try
        {
            runId = await startRunTask;
        }
        catch (OperationCanceledException ex)
        {
            attempt.AddWarning(ex);
            return Result.Cancel;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{ExecutionId} {Step} Error creating run for workspace {DatabricksWorkspaceId}",
                step.ExecutionId, step, step.DatabricksWorkspaceId);
            attempt.AddError(ex, "Error starting notebook run");
            return Result.Failure;
        }

        // Initialize timeout cancellation token source already here
        // so that we can start the countdown immediately after the job run is started.
        using var timeoutCts = step.TimeoutMinutes > 0
            ? new CancellationTokenSource(TimeSpan.FromMinutes(step.TimeoutMinutes))
            : new CancellationTokenSource();

        try
        {
            using var context = _dbContextFactory.CreateDbContext();
            attempt.JobRunId = runId;
            context.Attach(attempt);
            context.Entry(attempt).Property(e => e.JobRunId).IsModified = true;
            await context.SaveChangesAsync(CancellationToken.None);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "{ExecutionId} {Step} Error updating notebook job run id", step.ExecutionId, step);
            attempt.AddWarning(ex, $"Error updating notebook job run id {runId}");
        }

        Run run;
        try
        {
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);
            while (true)
            {
                run = await GetRunWithRetriesAsync(client, step, runId, linkedCts.Token);
                if (run.Status.State == RunStatusState.TERMINATED)
                {
                    break;
                }
                await Task.Delay(_pollingIntervalMs, linkedCts.Token);
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
            attempt.AddError(ex, "Error getting job run status");
            return Result.Failure;
        }

        var message = $"""
                Termination type: {run.Status.TerminationDetails.Type}
                Termination code: {run.Status.TerminationDetails.Code}
                Message: {run.Status.TerminationDetails.Message}
                """;
        if (run.Status.TerminationDetails.Type == RunTerminationType.SUCCESS)
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

    private async Task<Run> GetRunWithRetriesAsync(
        DatabricksClient client,
        DatabricksStepExecution step,
        long runId,
        CancellationToken cancellationToken)
    {
        var policy = Polly.Policy
            .Handle<Exception>()
            .WaitAndRetryAsync(
            retryCount: MaxRefreshRetries,
            sleepDurationProvider: retryCount => TimeSpan.FromMilliseconds(_pollingIntervalMs),
            onRetry: (ex, waitDuration) =>
                _logger.LogWarning(ex, "{ExecutionId} {Step} Error getting run status for run id {runId}", step.ExecutionId, step, runId));

        var (run, _) = await policy.ExecuteAsync((cancellationToken) =>
            client.Jobs.RunsGet(runId, cancellationToken: cancellationToken), cancellationToken);
        return run;
    }

    private async Task CancelAsync(
        DatabricksClient client,
        DatabricksStepExecution step,
        DatabricksStepExecutionAttempt attempt,
        long runId)
    {
        _logger.LogInformation("{ExecutionId} {Step} Stopping run id {runId}", step.ExecutionId, step, runId);
        try
        {
            await client.Jobs.RunsCancel(runId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{ExecutionId} {Step} Error stopping run {runId}", step.ExecutionId, step, runId);
            attempt.AddWarning(ex, $"Error stopping run {runId}");
        }
    }
}
