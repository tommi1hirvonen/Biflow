using Biflow.Executor.Core.Common;
using Microsoft.Azure.Databricks.Client;
using Microsoft.Azure.Databricks.Client.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;

namespace Biflow.Executor.Core.StepExecutor;

internal class DbNotebookStepExecutor(
    ILogger<DbNotebookStepExecutor> logger,
    IOptionsMonitor<ExecutionOptions> options,
    IDbContextFactory<ExecutorDbContext> dbContextFactory)
    : StepExecutor<DbNotebookStepExecution, DbNotebookStepExecutionAttempt>(logger, dbContextFactory)
{
    private readonly ILogger<DbNotebookStepExecutor> _logger = logger;
    private readonly IDbContextFactory<ExecutorDbContext> _dbContextFactory = dbContextFactory;
    private readonly int _pollingIntervalMs = options.CurrentValue.PollingIntervalMs;

    private const int MaxRefreshRetries = 3;

    protected override async Task<Result> ExecuteAsync(
        DbNotebookStepExecution step,
        DbNotebookStepExecutionAttempt attempt,
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

        var settings = new RunSubmitSettings
        {
            RunName = step.ExecutionId.ToString()
        };
        var task = new NotebookTask
        {
            NotebookPath = step.NotebookPath,
            BaseParameters = parameters
        };
        int? timeoutSeconds = step.TimeoutMinutes > 0 ? Convert.ToInt32(step.TimeoutMinutes * 60.0) : null;
        var taskSettings = settings.AddTask(step.StepId.ToString(), task, timeoutSeconds: timeoutSeconds);

        if (step.ClusterConfiguration is ExistingClusterConfiguration existing)
        {
            taskSettings.WithExistingClusterId(existing.ClusterId);
        }
        else if (step.ClusterConfiguration is NewClusterConfiguration newCluster)
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
        else
        {
            throw new ArgumentException($"Unhandled cluster configuration type {step.ClusterConfiguration.GetType()}");
        }

        long runId;
        try
        {
            runId = await client.Jobs.RunSubmit(settings);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{ExecutionId} {Step} Error creating dbnotebook run for workspace {DatabricksWorkspaceId} and notebook {NotebookPath}",
                step.ExecutionId, step, step.DatabricksWorkspaceId, step.NotebookPath);
            attempt.AddError(ex, "Error starting notebook run");
            return Result.Failure;
        }

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
            while (true)
            {
                run = await GetRunWithRetriesAsync(client, step, runId, cancellationToken);
                if (run.Status.State == RunStatusState.TERMINATED)
                {
                    break;
                }
                await Task.Delay(_pollingIntervalMs, cancellationToken);
            }
        }
        catch (OperationCanceledException ex)
        {
            await CancelAsync(client, step, attempt, runId);
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
        DbNotebookStepExecution step,
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
        DbNotebookStepExecution step,
        DbNotebookStepExecutionAttempt attempt,
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
