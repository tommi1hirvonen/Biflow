using Microsoft.Azure.Databricks.Client;
using Microsoft.Azure.Databricks.Client.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;

namespace Biflow.Executor.Core.StepExecutor;

internal class DatabricksStepExecutor(
    IServiceProvider serviceProvider,
    DatabricksStepExecution step,
    DatabricksStepExecutionAttempt attempt) : IStepExecutor, IDisposable
{
    private readonly ILogger<DatabricksStepExecutor> _logger = serviceProvider
        .GetRequiredService<ILogger<DatabricksStepExecutor>>();
    private readonly IDbContextFactory<ExecutorDbContext> _dbContextFactory = serviceProvider
        .GetRequiredService<IDbContextFactory<ExecutorDbContext>>();
    private readonly int _pollingIntervalMs = serviceProvider
        .GetRequiredService<IOptionsMonitor<ExecutionOptions>>()
        .CurrentValue
        .PollingIntervalMs;
    private readonly DatabricksClient _client =
        step.GetWorkspace()?.CreateClient().Client
        ?? throw new ArgumentNullException(message: "Step's databricks workspace is null", innerException: null);

    private const int MaxRefreshRetries = 3;

    public async Task<Result> ExecuteAsync(OrchestrationContext context, CancellationContext cancellationContext)
    {
        var cancellationToken = cancellationContext.CancellationToken;
        cancellationToken.ThrowIfCancellationRequested();

        // Get possible parameters.
        Dictionary<string, string> parameters;
        try
        {
            parameters = step.StepExecutionParameters
                .Where(p => p.ParameterValue.Value is not null)
                .ToDictionary(key => key.ParameterName, value => value.ParameterValue.ToString());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{ExecutionId} {Step} Error retrieving DbNotebook parameters", step.ExecutionId, step);
            attempt.AddError(ex, "Error reading DbNotebook parameters");
            return Result.Failure;
        }

        // Create run submit settings and the notebook task.

        Task<long> startRunTask;

        if (step.DatabricksStepSettings is DbJobStepSettings dbJob)
        {
            var runParams = new RunParameters { JobParams = parameters };
            startRunTask = _client.Jobs.RunNow(dbJob.JobId, runParams, cancellationToken: cancellationToken);
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

            switch (step.DatabricksStepSettings)
            {
                case DatabricksClusterStepSettings { ClusterConfiguration: ExistingClusterConfiguration existing }:
                    taskSettings.WithExistingClusterId(existing.ClusterId);
                    break;
                case DatabricksClusterStepSettings { ClusterConfiguration: NewClusterConfiguration newCluster }:
                    var cluster = ClusterAttributes.GetNewClusterConfiguration();
                    cluster = newCluster.ClusterMode switch
                    {
                        SingleNodeClusterConfiguration => cluster.WithClusterMode(ClusterMode.SingleNode),
                        FixedMultiNodeClusterConfiguration @fixed =>
                            cluster.WithClusterMode(ClusterMode.Standard).WithNumberOfWorkers(@fixed.NumberOfWorkers),
                        AutoscaleMultiNodeClusterConfiguration auto =>
                            cluster.WithClusterMode(ClusterMode.Standard).WithAutoScale(auto.MinimumWorkers, auto.MaximumWorkers),
                        _ => throw new ArgumentException($"Unhandled new cluster configuration type {newCluster.ClusterMode.GetType()}")
                    };
                    cluster = cluster
                        .WithNodeType(newCluster.NodeTypeId, newCluster.DriverNodeTypeId)
                        .WithRuntimeVersion(newCluster.RuntimeVersion)
                        .WithRuntimeEngine(newCluster.UsePhoton ? RuntimeEngine.PHOTON : RuntimeEngine.STANDARD);
                    taskSettings.WithNewCluster(cluster);
                    break;
            }

            startRunTask = _client.Jobs.RunSubmit(settings, cancellationToken: cancellationToken);
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
            await using var dbContext = await _dbContextFactory.CreateDbContextAsync(CancellationToken.None);
            attempt.JobRunId = runId;
            await dbContext.Set<DatabricksStepExecutionAttempt>()
                .Where(x => x.ExecutionId == attempt.ExecutionId && x.StepId == attempt.StepId && x.RetryAttemptIndex == attempt.RetryAttemptIndex)
                .ExecuteUpdateAsync(x => x
                    .SetProperty(p => p.JobRunId, attempt.JobRunId), CancellationToken.None);
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
                run = await GetRunWithRetriesAsync(runId, linkedCts.Token);
                if (run.Status.State == RunStatusState.TERMINATED)
                {
                    break;
                }
                await Task.Delay(_pollingIntervalMs, linkedCts.Token);
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
            attempt.AddError(ex, "Error getting job run status");
            return Result.Failure;
        }
        
        if (step.DatabricksStepSettings is DbJobStepSettings || run.Tasks.FirstOrDefault() is not { } task)
        {
            return run.Status.TerminationDetails.Type == RunTerminationType.SUCCESS
                ? Result.Success
                : Result.Failure;
        }
        
        // If the Databricks run was not a job run,
        // try to get the output for the one task in the one-time triggered run submit.
        try
        {
            var output = await _client.Jobs.RunsGetOutput(task.RunId, cancellationToken);
            if (!string.IsNullOrEmpty(output.Error))
            {
                attempt.AddError(output.Error);
            }
            if (!string.IsNullOrEmpty(output.Logs))
            {
                attempt.AddOutput(output.Logs[..Math.Min(500_000, output.Logs.Length)]);
                if (output.Logs.Length > 500_000)
                {
                    attempt.AddOutput("Output has been truncated to first 500 000 characters.", insertFirst: true);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "{ExecutionId} {Step} Error getting Databricks step task run output", step.ExecutionId, step);
            attempt.AddWarning(ex, "Error getting task run output");
        }

        return run.Status.TerminationDetails.Type == RunTerminationType.SUCCESS
            ? Result.Success
            : Result.Failure;
    }

    private async Task<Run> GetRunWithRetriesAsync(long runId, CancellationToken cancellationToken)
    {
        var policy = Polly.Policy.Handle<Exception>().WaitAndRetryAsync(
            retryCount: MaxRefreshRetries,
            sleepDurationProvider: retryCount => TimeSpan.FromMilliseconds(_pollingIntervalMs * retryCount),
            onRetry: (ex, _) => _logger.LogWarning(ex,
                "{ExecutionId} {Step} Error getting run status for run id {runId}", step.ExecutionId, step, runId));
        var (run, _) = await policy.ExecuteAsync(cancellation =>
            _client.Jobs.RunsGet(runId, cancellationToken: cancellation), cancellationToken);
        return run;
    }

    private async Task CancelAsync(long runId)
    {
        _logger.LogInformation("{ExecutionId} {Step} Stopping run id {runId}", step.ExecutionId, step, runId);
        try
        {
            await _client.Jobs.RunsCancel(runId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{ExecutionId} {Step} Error stopping run {runId}", step.ExecutionId, step, runId);
            attempt.AddWarning(ex, $"Error stopping run {runId}");
        }
    }

    public void Dispose() => _client.Dispose();
}
