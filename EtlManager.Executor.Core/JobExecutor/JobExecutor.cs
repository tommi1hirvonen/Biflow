using EtlManager.DataAccess;
using EtlManager.DataAccess.Models;
using EtlManager.Executor.Core.Notification;
using EtlManager.Executor.Core.Orchestrator;
using Microsoft.EntityFrameworkCore;
using Serilog;
using System.Diagnostics;
using System.Text.Json;

namespace EtlManager.Executor.Core.JobExecutor;

internal class JobExecutor : IJobExecutor
{
    private readonly IDbContextFactory<EtlManagerContext> _dbContextFactory;
    private readonly INotificationService _notificationService;
    private readonly IOrchestratorFactory _orchestratorFactory;

    private OrchestratorBase? Orchestrator { get; set; }

    public JobExecutor(
        IDbContextFactory<EtlManagerContext> dbContextFactory,
        INotificationService notificationService,
        IOrchestratorFactory orchestratorFactory)
    {
        _dbContextFactory = dbContextFactory;
        _notificationService = notificationService;
        _orchestratorFactory = orchestratorFactory;
    }

    public async Task RunAsync(Guid executionId, bool notify, SubscriptionType? notifyMe, bool notifyMeOvertime)
    {
        Execution execution;
        Job job;
        using (var context = _dbContextFactory.CreateDbContext())
        {
            try
            {
                var process = Process.GetCurrentProcess();
                execution = await context.Executions
                    .AsNoTrackingWithIdentityResolution()
                    .Include(e => e.Job)
                    .Include(e => e.ExecutionParameters)
                    .Include(e => e.StepExecutions)
                    .ThenInclude(e => e.StepExecutionAttempts)
                    .Include(e => e.StepExecutions)
                    .ThenInclude(e => e.ExecutionDependencies)
                    .ThenInclude(e => e.DependantOnStepExecution)
                    .Include(e => e.StepExecutions)
                    .ThenInclude(e => (e as ParameterizedStepExecution)!.StepExecutionParameters)
                    .ThenInclude(p => p.ExecutionParameter)
                    .Include(e => e.StepExecutions)
                    .ThenInclude(e => (e as DatasetStepExecution)!.AppRegistration)
                    .Include(e => e.StepExecutions)
                    .ThenInclude(e => (e as FunctionStepExecution)!.FunctionApp)
                    .Include(e => e.StepExecutions)
                    .ThenInclude(e => (e as PipelineStepExecution)!.DataFactory)
                    .ThenInclude(df => df.AppRegistration)
                    .Include(e => e.StepExecutions)
                    .ThenInclude(e => (e as SqlStepExecution)!.Connection)
                    .Include(e => e.StepExecutions)
                    .ThenInclude(e => (e as PackageStepExecution)!.Connection)
                    .Include(e => e.StepExecutions)
                    .ThenInclude(e => (e as AgentJobStepExecution)!.Connection)
                    .Include(e => e.StepExecutions)
                    .ThenInclude(e => (e as TabularStepExecution)!.Connection)
                    .FirstAsync(e => e.ExecutionId == executionId);

                job = execution.Job ?? throw new InvalidOperationException("Job was null");

                execution.ExecutorProcessId = process.Id;
                execution.ExecutionStatus = ExecutionStatus.Running;
                execution.StartDateTime = DateTimeOffset.Now;
                context.Attach(execution);
                context.Entry(execution).Property(e => e.ExecutorProcessId).IsModified = true;
                context.Entry(execution).Property(e => e.ExecutionStatus).IsModified = true;
                context.Entry(execution).Property(e => e.StartDateTime).IsModified = true;
                await context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error getting job details for execution");
                return;
            }
        }

        // Check whether there are circular dependencies between jobs (through steps executing another jobs).
        string? circularExecutions;
        try
        {
            circularExecutions = await GetCircularJobExecutionsAsync(job);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "{executionId} Error checking for possible circular job executions", executionId);
            return;
        }

        if (!string.IsNullOrEmpty(circularExecutions))
        {
            var errorMessage = "Execution was cancelled because of circular job executions:\n" + circularExecutions;
            using var context = _dbContextFactory.CreateDbContext();
            foreach (var attempt in execution.StepExecutions.SelectMany(e => e.StepExecutionAttempts))
            {
                attempt.StartDateTime = DateTimeOffset.Now;
                attempt.EndDateTime = DateTimeOffset.Now;
                attempt.ErrorMessage = errorMessage;
                attempt.ExecutionStatus = StepExecutionStatus.Failed;
                context.Attach(attempt).State = EntityState.Modified;
            }

            execution.StartDateTime = DateTimeOffset.Now;
            execution.EndDateTime = DateTimeOffset.Now;
            execution.ExecutionStatus = ExecutionStatus.Failed;
            context.Attach(execution).State = EntityState.Modified;

            await context.SaveChangesAsync();

            Log.Error("{executionId} Execution was cancelled because of circular job executions: " + circularExecutions, executionId);
            return;
        }

        Orchestrator = _orchestratorFactory.Create(execution);

        try
        {
            var notificationTask = execution.OvertimeNotificationLimitMinutes > 0
                ? Task.Delay(TimeSpan.FromMinutes(execution.OvertimeNotificationLimitMinutes))
                : Task.Delay(-1); // infinite timeout

            var orchestrationTask = Orchestrator.RunAsync();

            await Task.WhenAny(notificationTask, orchestrationTask);

            // If the notification task completed first and notify is true,
            // send a notification about a long running execution.
            if (notificationTask.IsCompleted)
            {
                await _notificationService.SendLongRunningExecutionNotification(execution, notify, notifyMeOvertime);
            }

            await orchestrationTask; // Wait for orchestration to finish.
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error during job execution");
        }

        // Get job execution status based on step execution statuses.
        var allStepAttempts = execution.StepExecutions.SelectMany(e => e.StepExecutionAttempts).ToList();
        ExecutionStatus status;
        if (allStepAttempts.All(step => step.ExecutionStatus == StepExecutionStatus.Succeeded))
            status = ExecutionStatus.Succeeded;
        else if (allStepAttempts.Any(step => step.ExecutionStatus == StepExecutionStatus.Failed))
            status = ExecutionStatus.Failed;
        else if (allStepAttempts.Any(step => step.ExecutionStatus == StepExecutionStatus.AwaitRetry || step.ExecutionStatus == StepExecutionStatus.Duplicate))
            status = ExecutionStatus.Warning;
        else if (allStepAttempts.Any(step => step.ExecutionStatus == StepExecutionStatus.Stopped))
            status = ExecutionStatus.Stopped;
        else if (allStepAttempts.Any(step => step.ExecutionStatus == StepExecutionStatus.NotStarted))
            status = ExecutionStatus.Suspended;
        else
            status = ExecutionStatus.Failed;

        // Update job execution status.
        try
        {
            using var context = _dbContextFactory.CreateDbContext();
            execution.ExecutionStatus = status;
            execution.EndDateTime = DateTimeOffset.Now;
            context.Attach(execution);
            context.Entry(execution).Property(e => e.ExecutionStatus).IsModified = true;
            context.Entry(execution).Property(e => e.EndDateTime).IsModified = true;
            await context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error updating execution status");
        }

        await _notificationService.SendCompletionNotification(execution, notify, notifyMe);
    }

    public void Cancel(string username) => Orchestrator?.CancelExecution(username);

    public void Cancel(string username, Guid stepId) => Orchestrator?.CancelExecution(username, stepId);

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
        var dependencies = await ReadDependenciesAsync();
        List<List<Job>> cycles = dependencies.FindCycles();
        var jobs = cycles.Select(c => c.Select(c_ => new { c_.JobId, c_.JobName })).ToList();
        var encoder = System.Text.Encodings.Web.JavaScriptEncoder.Create(System.Text.Unicode.UnicodeRanges.All);
        var json = JsonSerializer.Serialize(jobs, new JsonSerializerOptions { WriteIndented = true, Encoder = encoder });

        // There are no circular dependencies or this job is not among the cycles.
        return cycles.Count == 0 || !cycles.Any(jobs => jobs.Any(j => j.JobId == job.JobId))
            ? null : json;
    }

    private async Task<Dictionary<Job, List<Job>>> ReadDependenciesAsync()
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
            .ToListAsync();
        var dependencies = steps
            .GroupBy(key => key.Job, element => element.JobToExecute)
            .ToDictionary(grouping => grouping.Key, grouping => grouping.ToList());
        return dependencies;
    }

}
