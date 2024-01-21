using Biflow.Executor.Core.Notification;
using Biflow.Executor.Core.Orchestrator;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;

namespace Biflow.Executor.Core.JobExecutor;

internal class JobExecutor(
    ILogger<JobExecutor> logger,
    IDbContextFactory<ExecutorDbContext> dbContextFactory,
    INotificationService notificationService,
    IJobOrchestratorFactory jobOrchestratorFactory,
    Execution execution) : IJobExecutor
{
    private readonly ILogger<JobExecutor> _logger = logger;
    private readonly IDbContextFactory<ExecutorDbContext> _dbContextFactory = dbContextFactory;
    private readonly INotificationService _notificationService = notificationService;
    private readonly IJobOrchestrator _jobOrchestrator = jobOrchestratorFactory.Create(execution);
    private readonly Execution _execution = execution;
    private readonly JsonSerializerOptions _serializerOptions =
        new() { WriteIndented = true, Encoder = JavaScriptEncoder.Create(UnicodeRanges.All) };

    public async Task RunAsync(Guid executionId, CancellationToken cancellationToken)
    {
        // CancellationToken is triggered when the executor service is being shut down
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            var process = Process.GetCurrentProcess();
            using var context = _dbContextFactory.CreateDbContext();
            context.Attach(_execution);
            _execution.ExecutorProcessId = process.Id;
            _execution.ExecutionStatus = ExecutionStatus.Running;
            _execution.StartedOn = DateTimeOffset.Now;
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating job execution status");
            return;
        }

        // Check whether there are circular dependencies between jobs (through steps executing another jobs).
        string? circularExecutions;
        try
        {
            circularExecutions = await GetCircularJobExecutionsAsync(_execution.JobId, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{executionId} Error checking for possible circular job executions", executionId);
            await UpdateExecutionFailedAsync("Error checking for possible circular job executions");
            return;
        }

        if (!string.IsNullOrEmpty(circularExecutions))
        {
            var errorMessage = "Execution was cancelled because of circular job executions:\n" + circularExecutions;
            await UpdateExecutionFailedAsync(errorMessage);
            _logger.LogError("{executionId} Execution was cancelled because of circular job executions: {circularExecutions}", executionId, circularExecutions);
            return;
        }

        // Check whether there are circular dependencies between steps.
        string? circularSteps;
        try
        {
            circularSteps = await GetCircularStepDependenciesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{executionId} Error checking for possible circular step dependencies", executionId);
            await UpdateExecutionFailedAsync("Error checking for possible circular step dependencies");
            return;
        }

        if (!string.IsNullOrEmpty(circularSteps))
        {
            var errorMessage = "Execution was cancelled because of circular step dependencies:\n" + circularSteps;
            await UpdateExecutionFailedAsync(errorMessage);
            _logger.LogError("{executionId} Execution was cancelled because of circular step dependencies: {circularSteps}", executionId, circularSteps);
            return;
        }

        // Update execution parameter values for parameters that use expressions.
        try
        {
            using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
            foreach (var parameter in _execution.ExecutionParameters.Where(p => p.UseExpression))
            {
                context.Attach(parameter);
                parameter.ParameterValue = await parameter.EvaluateAsync();
            }
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{executionId} Error evaluating execution parameters and saving evaluation results", executionId);
            await UpdateExecutionFailedAsync("Error evaluating execution parameters and saving evaluation results");
            return;
        }

        try
        {
            // Don't pass cancellation token to notification task to not needlessly send notifications
            // if service is being shut down. Instead, pass new token which can be canceled if timeout is not reached.
            using var notificationCts = new CancellationTokenSource();
            var notificationTask = _execution.OvertimeNotificationLimitMinutes > 0
                ? Task.Delay(TimeSpan.FromMinutes(_execution.OvertimeNotificationLimitMinutes), notificationCts.Token)
                : Task.Delay(-1, notificationCts.Token); // infinite timeout

            var orchestrationTask = _jobOrchestrator.RunAsync(cancellationToken);

            await Task.WhenAny(notificationTask, orchestrationTask);

            // If the notification task completed first, send long running notification.
            if (notificationTask.IsCompleted)
            {
                await _notificationService.SendLongRunningExecutionNotificationAsync(_execution);
            }
            notificationCts.Cancel();
            await orchestrationTask; // Wait for orchestration to finish.
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during job execution");
        }

        // Update job execution status.
        try
        {
            using var context = _dbContextFactory.CreateDbContext();
            context.Attach(_execution);
            _execution.ExecutionStatus = _execution.GetCalculatedStatus();
            _execution.EndedOn = DateTimeOffset.Now;
            await context.SaveChangesAsync(CancellationToken.None);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating execution status");
        }

        // In case of cancellation (service shutdown), do not send notifications.
        cancellationToken.ThrowIfCancellationRequested();

        await _notificationService.SendCompletionNotificationAsync(_execution);
    }

    public void Cancel(string username) => _jobOrchestrator?.CancelExecution(username);

    public void Cancel(string username, Guid stepId) => _jobOrchestrator?.CancelExecution(username, stepId);

    /// <summary>
    /// Checks for circular dependencies between jobs.
    /// Jobs can reference other jobs, so it's important to check them for circlular dependencies.
    /// </summary>
    /// <param name="executionConfig"></param>
    /// <returns>
    /// JSON string of circular job dependencies or null if there were no circular dependencies.
    /// </returns>
    private async Task<string?> GetCircularJobExecutionsAsync(Guid jobId, CancellationToken cancellationToken)
    {
        var dependencies = await ReadJobDependenciesAsync(cancellationToken);
        IEnumerable<IEnumerable<JobProjection>> cycles = dependencies.FindCycles();
        var json = JsonSerializer.Serialize(cycles, _serializerOptions);

        // There are no circular dependencies or this job is not among the cycles.
        return !cycles.Any() || !cycles.Any(jobs => jobs.Any(j => j.JobId == jobId))
            ? null : json;
    }

    private async Task<string?> GetCircularStepDependenciesAsync(CancellationToken cancellationToken)
    {
        // Find circular step dependencies which are not allowed since they would block each other's executions.
        var dependencies = await ReadStepDependenciesAsync(cancellationToken);
        IEnumerable<IEnumerable<StepProjection>> cycles = dependencies.FindCycles();
        var json = JsonSerializer.Serialize(cycles, _serializerOptions);
        return !cycles.Any() ? null : json;
    }

    private async Task<Dictionary<JobProjection, JobProjection[]>> ReadJobDependenciesAsync(CancellationToken cancellationToken)
    {
        using var context = _dbContextFactory.CreateDbContext();
        var jobs = await context.JobSteps
            .AsNoTracking()
            .Select(step => new
            {
                Job = new JobProjection(step.Job.JobId, step.Job.JobName),
                JobToExecute = new JobProjection(step.JobToExecute.JobId, step.JobToExecute.JobName)
            })
            .ToArrayAsync(cancellationToken);
        var dependencies = jobs
            .GroupBy(key => key.Job, element => element.JobToExecute)
            .ToDictionary(grouping => grouping.Key, grouping => grouping.ToArray());
        return dependencies;
    }

    private async Task<Dictionary<StepProjection, StepProjection[]>> ReadStepDependenciesAsync(CancellationToken cancellationToken)
    {
        using var context = _dbContextFactory.CreateDbContext();
        var steps = await context.Dependencies
            .AsNoTracking()
            .Where(d => d.Step.JobId == _execution.JobId)
            .Select(d => new
            {
                Step = new StepProjection(d.Step.StepId, d.Step.StepName),
                DependantOnStep = new StepProjection(d.DependantOnStep.StepId, d.DependantOnStep.StepName)
            })
            .ToArrayAsync(cancellationToken);
        var dependencies = steps
            .GroupBy(key => key.Step, element => element.DependantOnStep)
            .ToDictionary(g => g.Key, g => g.ToArray());
        return dependencies;
    }

    private async Task UpdateExecutionFailedAsync(string errorMessage)
    {
        using var context = _dbContextFactory.CreateDbContext();
        
        foreach (var attempt in _execution.StepExecutions.SelectMany(s => s.StepExecutionAttempts))
        {
            context.Attach(attempt);
            attempt.StartedOn = DateTimeOffset.Now;
            attempt.EndedOn = DateTimeOffset.Now;
            attempt.ExecutionStatus = StepExecutionStatus.Failed;
            attempt.AddError(errorMessage);
        }

        context.Attach(_execution);
        _execution.StartedOn = DateTimeOffset.Now;
        _execution.EndedOn = DateTimeOffset.Now;
        _execution.ExecutionStatus = ExecutionStatus.Failed;
        // Do not cancel saving failed status.
        await context.SaveChangesAsync();
    }

    private record JobProjection(Guid JobId, string JobName);

    private record StepProjection(Guid StepId, string? StepName);
}
