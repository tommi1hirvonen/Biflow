using Dapper;
using EtlManagerDataAccess;
using EtlManagerDataAccess.Models;
using EtlManagerUtils;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Serilog;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace EtlManagerExecutor
{
    class JobExecutor : IJobExecutor
    {
        private readonly IConfiguration configuration;
        private readonly IDbContextFactory<EtlManagerContext> dbContextFactory;

        public JobExecutor(IConfiguration configuration, IDbContextFactory<EtlManagerContext> dbContextFactory)
        {
            this.configuration = configuration;
            this.dbContextFactory = dbContextFactory;
        }

        public async Task RunAsync(Guid executionId, bool notify)
        {
            var connectionString = configuration.GetConnectionString("EtlManagerContext");
            var pollingIntervalMs = configuration.GetValue<int>("PollingIntervalMs");
            var maxParallelSteps = configuration.GetValue<int>("MaximumParallelSteps");
            var encryptionId = configuration.GetValue<string>("EncryptionId");

            string? encryptionPassword;
            try
            {
                encryptionPassword = await CommonUtility.GetEncryptionKeyAsync(encryptionId, connectionString);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "{executionId} Error getting encryption password", executionId);
                return;
            }

            Execution execution;
            Job job;
            using (var context = dbContextFactory.CreateDbContext())
            {
                try
                {
                    var process = Process.GetCurrentProcess();
                    execution = await context.Executions
                        .AsNoTrackingWithIdentityResolution()
                        .Include(e => e.StepExecutions)
                        .ThenInclude(e => e.StepExecutionAttempts)
                        .Include(e => e.StepExecutions)
                        .ThenInclude(e => (e as ParameterizedStepExecution)!.StepExecutionParameters)
                        .FirstAsync(e => e.ExecutionId == executionId);
                    
                    execution.ExecutorProcessId = process.Id;
                    execution.ExecutionStatus = ExecutionStatus.Running;
                    execution.StartDateTime = DateTime.Now;
                    context.Attach(execution);
                    context.Entry(execution).Property(e => e.ExecutorProcessId).IsModified = true;
                    context.Entry(execution).Property(e => e.ExecutionStatus).IsModified = true;
                    context.Entry(execution).Property(e => e.StartDateTime).IsModified = true;
                    await context.SaveChangesAsync();
                    
                    job = await context.Jobs
                        .AsNoTrackingWithIdentityResolution()
                        .FirstAsync(j => j.JobId == execution.JobId);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Error getting job details for execution");
                    return;
                }
            }

            var executionConfig = new ExecutionConfiguration(
                dbContextFactory: dbContextFactory,
                connectionString: connectionString,
                encryptionKey: encryptionPassword,
                maxParallelSteps: maxParallelSteps,
                pollingIntervalMs: pollingIntervalMs,
                executionId: executionId,
                job: job,
                notify: notify,
                // Set the username as timeout. If steps are to be canceled, this will be used by default.
                username: "timeout");

            // Check whether there are circular dependencies between jobs (through steps executing another jobs).
            string? circularExecutions;
            try
            {
                circularExecutions = await GetCircularJobExecutionsAsync(executionConfig);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "{executionId} Error checking for possible circular job executions", executionId);
                return;
            }

            if (!string.IsNullOrEmpty(circularExecutions))
            {
                await Utility.UpdateErrorMessageAsync(executionConfig, "Execution was cancelled because of circular job executions:\n" + circularExecutions);
                Log.Error("{executionId} Execution was cancelled because of circular job executions: " + circularExecutions, executionId);
                return;
            }

            ExecutorBase executor;
            if (execution.DependencyMode)
            {
                Log.Information("{ExecutionId} Starting execution in dependency mode", executionId);
                executor = new DependencyModeExecutor(executionConfig, execution);
            }
            else
            {
                Log.Information("{executionId} Starting execution in execution phase mode", executionId);
                executor = new ExecutionPhaseExecutor(executionConfig, execution);
            }

            try
            {
                await executor.RunAsync();
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
                using var context = dbContextFactory.CreateDbContext();
                execution.ExecutionStatus = status;
                execution.EndDateTime = DateTime.Now;
                context.Attach(execution);
                context.Entry(execution).Property(e => e.ExecutionStatus).IsModified = true;
                context.Entry(execution).Property(e => e.EndDateTime).IsModified = true;
                await context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error updating execution status");
            }

            // Execution finished. Notify subscribers of possible errors.
            if (notify)
            {
                EmailHelper.SendNotification(configuration, dbContextFactory, executionId);
            }
        }

        /// <summary>
        /// Checks for circular dependencies between jobs.
        /// Jobs can reference other jobs, so it's important to check them for circlular dependencies.
        /// </summary>
        /// <param name="executionConfig"></param>
        /// <returns>
        /// JSON string of circular job dependencies or null if there were no circular dependencies.
        /// </returns>
        private async Task<string?> GetCircularJobExecutionsAsync(ExecutionConfiguration executionConfig)
        {
            var dependencies = await ReadDependenciesAsync();
            List<List<Job>> cycles = dependencies.FindCycles();

            var encoder = System.Text.Encodings.Web.JavaScriptEncoder.Create(System.Text.Unicode.UnicodeRanges.All);
            var json = JsonSerializer.Serialize(cycles, new JsonSerializerOptions { WriteIndented = true, Encoder = encoder });

            // There are no circular dependencies or this job is not among the cycles.
            return cycles.Count == 0 || !cycles.Any(jobs => jobs.Any(job => job.JobId == executionConfig.Job.JobId))
                ? null : json;
        }

        private async Task<Dictionary<Job, List<Job>>> ReadDependenciesAsync()
        {
            using var context = dbContextFactory.CreateDbContext();
            var steps = await context.JobSteps
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
           
}
