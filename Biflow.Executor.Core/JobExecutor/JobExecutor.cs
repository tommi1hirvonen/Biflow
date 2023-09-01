using Biflow.DataAccess.Models;
using Biflow.Executor.Core.Notification;
using Biflow.Executor.Core.Orchestrator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Text.Json;

namespace Biflow.Executor.Core.JobExecutor;

internal class JobExecutor : IJobExecutor
{
    private readonly ILogger<JobExecutor> _logger;
    private readonly IDbContextFactory<ExecutorDbContext> _dbContextFactory;
    private readonly INotificationService _notificationService;
    private readonly IJobOrchestrator _jobOrchestrator;
    private readonly Execution _execution;

    private Job Job => _execution.Job ?? throw new ArgumentNullException(nameof(_execution.Job));

    public JobExecutor(
        ILogger<JobExecutor> logger,
        IDbContextFactory<ExecutorDbContext> dbContextFactory,
        INotificationService notificationService,
        IJobOrchestratorFactory jobOrchestratorFactory,
        Execution execution)
    {
        _logger = logger;
        _dbContextFactory = dbContextFactory;
        _notificationService = notificationService;
        _execution = execution;
        _jobOrchestrator = jobOrchestratorFactory.Create(execution);
    }

    public async Task RunAsync(Guid executionId)
    {
        try
        {
            var process = Process.GetCurrentProcess();
            _execution.ExecutorProcessId = process.Id;
            _execution.ExecutionStatus = ExecutionStatus.Running;
            _execution.StartDateTime = DateTimeOffset.Now;
            using var context = _dbContextFactory.CreateDbContext();
            context.Attach(_execution);
            context.Entry(_execution).Property(e => e.ExecutorProcessId).IsModified = true;
            context.Entry(_execution).Property(e => e.ExecutionStatus).IsModified = true;
            context.Entry(_execution).Property(e => e.StartDateTime).IsModified = true;
            await context.SaveChangesAsync();
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
            circularExecutions = await GetCircularJobExecutionsAsync(Job);
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
            circularSteps = await GetCircularStepDependenciesAsync();
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
            using var context = await _dbContextFactory.CreateDbContextAsync();
            foreach (var parameter in _execution.ExecutionParameters.Where(p => p.UseExpression))
            {
                context.Attach(parameter);
                parameter.ParameterValue = await parameter.EvaluateAsync();
            }
            await context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{executionId} Error evaluating execution parameters and saving evaluation results", executionId);
            await UpdateExecutionFailedAsync("Error evaluating execution parameters and saving evaluation results");
            return;
        }

        try
        {
            var notificationTask = _execution.OvertimeNotificationLimitMinutes > 0
                ? Task.Delay(TimeSpan.FromMinutes(_execution.OvertimeNotificationLimitMinutes))
                : Task.Delay(-1); // infinite timeout

            var orchestrationTask = _jobOrchestrator.RunAsync();

            await Task.WhenAny(notificationTask, orchestrationTask);

            // If the notification task completed first and notify is true,
            // send a notification about a long running execution.
            if (notificationTask.IsCompleted)
            {
                await _notificationService.SendLongRunningExecutionNotification(_execution);
            }

            await orchestrationTask; // Wait for orchestration to finish.
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during job execution");
        }

        // Get job execution status based on step execution statuses.
        var allStepAttempts = _execution.StepExecutions.SelectMany(e => e.StepExecutionAttempts).ToList();
        ExecutionStatus status;
        if (allStepAttempts.All(step => step.ExecutionStatus == StepExecutionStatus.Succeeded
            || step.ExecutionStatus == StepExecutionStatus.Skipped
            || step.ExecutionStatus == StepExecutionStatus.DependenciesFailed))
        {
            status = ExecutionStatus.Succeeded;
        }
        else if (allStepAttempts.Any(step => step.ExecutionStatus == StepExecutionStatus.Failed))
        {
            status = ExecutionStatus.Failed;
        }
        else if (allStepAttempts.Any(step => step.ExecutionStatus == StepExecutionStatus.Retry
            || step.ExecutionStatus == StepExecutionStatus.Duplicate
            || step.ExecutionStatus == StepExecutionStatus.Warning))
        {
            status = ExecutionStatus.Warning;
        }
        else if (allStepAttempts.Any(step => step.ExecutionStatus == StepExecutionStatus.Stopped))
        {
            status = ExecutionStatus.Stopped;
        }
        else if (allStepAttempts.Any(step => step.ExecutionStatus == StepExecutionStatus.NotStarted
            || step.ExecutionStatus == StepExecutionStatus.Queued
            || step.ExecutionStatus == StepExecutionStatus.AwaitingRetry))
        {
            status = ExecutionStatus.Suspended;
        }
        else
        {
            status = ExecutionStatus.Failed;
        }

        // Update job execution status.
        try
        {
            using var context = _dbContextFactory.CreateDbContext();
            _execution.ExecutionStatus = status;
            _execution.EndDateTime = DateTimeOffset.Now;
            context.Attach(_execution);
            context.Entry(_execution).Property(e => e.ExecutionStatus).IsModified = true;
            context.Entry(_execution).Property(e => e.EndDateTime).IsModified = true;
            await context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating execution status");
        }

        await _notificationService.SendCompletionNotification(_execution);
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
    private async Task<string?> GetCircularJobExecutionsAsync(Job job)
    {
        var dependencies = await ReadJobDependenciesAsync();
        IEnumerable<IEnumerable<Job>> cycles = dependencies.FindCycles();
        var jobs = cycles
            .Select(c => c.Select(c_ => new { c_.JobId, c_.JobName }).ToArray())
            .ToArray();
        var encoder = System.Text.Encodings.Web.JavaScriptEncoder.Create(System.Text.Unicode.UnicodeRanges.All);
        var json = JsonSerializer.Serialize(jobs, new JsonSerializerOptions { WriteIndented = true, Encoder = encoder });

        // There are no circular dependencies or this job is not among the cycles.
        return !cycles.Any() || !cycles.Any(jobs => jobs.Any(j => j.JobId == job.JobId))
            ? null : json;
    }

    private async Task<string?> GetCircularStepDependenciesAsync()
    {
        // Find circular step dependencies which are not allowed since they would block each other's executions.
        var dependencies = await ReadStepDependenciesAsync();
        IEnumerable<IEnumerable<Step>> cycles = dependencies.FindCycles();
        var steps = cycles
            .Select(c1 => c1.Select(c2 => new { c2.StepId, c2.StepName }).ToArray())
            .ToArray();

        var encoder = System.Text.Encodings.Web.JavaScriptEncoder.Create(System.Text.Unicode.UnicodeRanges.All);
        var json = JsonSerializer.Serialize(steps, new JsonSerializerOptions { WriteIndented = true, Encoder = encoder });

        return !cycles.Any() ? null : json;
    }

    private async Task<Dictionary<Job, Job[]>> ReadJobDependenciesAsync()
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
            .ToArrayAsync();
        var dependencies = steps
            .GroupBy(key => key.Job, element => element.JobToExecute)
            .ToDictionary(grouping => grouping.Key, grouping => grouping.ToArray());
        return dependencies;
    }

    private async Task<Dictionary<Step, Step[]>> ReadStepDependenciesAsync()
    {
        using var context = _dbContextFactory.CreateDbContext();
        var steps = await context.Steps
            .AsNoTrackingWithIdentityResolution()
            .Where(step => step.JobId == _execution.JobId)
            .Include(step => step.Dependencies)
            .ThenInclude(d => d.DependantOnStep)
            .ToArrayAsync();
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
        await context.StepExecutionAttempts
            .Where(e => e.ExecutionId == _execution.ExecutionId)
            .ExecuteUpdateAsync(attempt => attempt
            .SetProperty(p => p.StartDateTime, DateTimeOffset.Now)
            .SetProperty(p => p.EndDateTime, DateTimeOffset.Now)
            .SetProperty(p => p.ErrorMessage, errorMessage)
            .SetProperty(p => p.ExecutionStatus, StepExecutionStatus.Failed));

        _execution.StartDateTime = DateTimeOffset.Now;
        _execution.EndDateTime = DateTimeOffset.Now;
        _execution.ExecutionStatus = ExecutionStatus.Failed;
        context.Attach(_execution).State = EntityState.Modified;

        await context.SaveChangesAsync();
    }

}
