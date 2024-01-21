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
        IEnumerable<IEnumerable<Job>> cycles = dependencies.FindCycles();
        var jobs = cycles
            .Select(c => c.Select(c_ => new { c_.JobId, c_.JobName }).ToArray())
            .ToArray();
        var json = JsonSerializer.Serialize(jobs, _serializerOptions);

        // There are no circular dependencies or this job is not among the cycles.
        return !cycles.Any() || !cycles.Any(jobs => jobs.Any(j => j.JobId == jobId))
            ? null : json;
    }

    private async Task<string?> GetCircularStepDependenciesAsync(CancellationToken cancellationToken)
    {
        // Find circular step dependencies which are not allowed since they would block each other's executions.
        var dependencies = await ReadStepDependenciesAsync(cancellationToken);
        IEnumerable<IEnumerable<Step>> cycles = dependencies.FindCycles();
        var steps = cycles
            .Select(c1 => c1.Select(c2 => new { c2.StepId, c2.StepName }).ToArray())
            .ToArray();
        var json = JsonSerializer.Serialize(steps, _serializerOptions);
        return !cycles.Any() ? null : json;
    }

    private async Task<Dictionary<Job, Job[]>> ReadJobDependenciesAsync(CancellationToken cancellationToken)
    {
        using var context = _dbContextFactory.CreateDbContext();
        var steps = await context.JobSteps
            .AsNoTrackingWithIdentityResolution()
            .Include(step => step.Job)
            .Include(step => step.JobToExecute)
            .Select(step => new
            {
                step.Job,
                step.JobToExecute
            })
            .ToArrayAsync(cancellationToken);
        var dependencies = steps
            .GroupBy(key => key.Job, element => element.JobToExecute)
            .ToDictionary(grouping => grouping.Key, grouping => grouping.ToArray());
        return dependencies;
    }

    private async Task<Dictionary<Step, Step[]>> ReadStepDependenciesAsync(CancellationToken cancellationToken)
    {
        using var context = _dbContextFactory.CreateDbContext();
        var steps = await context.Steps
            .AsNoTrackingWithIdentityResolution()
            .Where(step => step.JobId == _execution.JobId)
            .Include(step => step.Dependencies)
            .ThenInclude(d => d.DependantOnStep)
            .ToArrayAsync(cancellationToken);
        var dependencies = steps
            .SelectMany(step => step.Dependencies)
            .Select(d => new { d.Step, d.DependantOnStep})
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

}
