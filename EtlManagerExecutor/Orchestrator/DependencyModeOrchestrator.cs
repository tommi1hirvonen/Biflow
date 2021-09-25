using EtlManagerDataAccess;
using EtlManagerDataAccess.Models;
using Microsoft.EntityFrameworkCore;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace EtlManagerExecutor
{
    class DependencyModeOrchestrator : OrchestratorBase
    {
        private readonly IDbContextFactory<EtlManagerContext> _dbContextFactory;

        private List<Task> StepWorkers { get; } = new();

        public DependencyModeOrchestrator(
            IExecutionConfiguration executionConfiguration,
            IStepExecutorFactory stepExecutorFactory,
            IDbContextFactory<EtlManagerContext> dbContextFactory,
            Execution execution)
            : base(executionConfiguration, stepExecutorFactory, execution)
        {
            _dbContextFactory = dbContextFactory;
        }

        public override async Task RunAsync()
        {
            try
            {
                // Find circular step dependencies which are not allowed since they would block each other's executions.
                var cycles = Execution.StepExecutions
                    .Where(e => e.ExecutionDependencies.Any())
                    .ToDictionary(
                    e => e,
                    e => e.ExecutionDependencies.Select(d => d.DependantOnStepExecution))
                    .FindCycles();

                // If there are circular dependencies, update error message for all steps and cancel execution.
                if (cycles.Count > 0)
                {
                    var steps = cycles.Select(c => c.Select(c_ => new { c_.StepId, c_.StepName }).ToList()).ToList();
                    var encoder = System.Text.Encodings.Web.JavaScriptEncoder.Create(System.Text.Unicode.UnicodeRanges.All);
                    var json = JsonSerializer.Serialize(steps, new JsonSerializerOptions { WriteIndented = true, Encoder = encoder });
                    var errorMessage = "Execution was cancelled because of circular step dependencies:\n" + json;

                    using var context = _dbContextFactory.CreateDbContext();
                    foreach (var attempt in Execution.StepExecutions.SelectMany(e => e.StepExecutionAttempts))
                    {
                        attempt.StartDateTime = DateTimeOffset.Now;
                        attempt.EndDateTime = DateTimeOffset.Now;
                        attempt.ErrorMessage = errorMessage;
                        attempt.ExecutionStatus = StepExecutionStatus.Failed;
                        context.Attach(attempt).State = EntityState.Modified;
                    }
                    await context.SaveChangesAsync();

                    Log.Error("{ExecutionId} Execution was cancelled because of circular step dependencies: " + json, Execution.ExecutionId);
                    return;
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "{ExecutionId} Error checking for possible circular step dependencies", Execution.ExecutionId);
                return;
            }

            // Start listening for cancel commands.
            RegisterCancelListeners();

            // Loop as long as there are steps that haven't yet been started.
            while (StepStatuses.Any(step => step.Value == ExecutionStatus.NotStarted))
            {
                // Start steps that can be started and skip those that should be skipped.
                await DoRoundAsync();
                // Wait for at least one step to finish before doing another round.
                await Task.WhenAny(StepWorkers);
                // Remove finished tasks from the list so that they don't immediately trigger the next Task.WhenAny().
                StepWorkers.RemoveAll(task => task.IsCompleted);
            }

            // All steps have been started. Wait until the remaining step worker tasks have finished.
            await Task.WhenAll(StepWorkers);
        }

        private async Task DoRoundAsync()
        {
            var unstartedSteps = StepStatuses
                .Where(status => status.Value == ExecutionStatus.NotStarted)
                .Select(status => status.Key);
            foreach (var step in unstartedSteps)
            {
                var stepAction = GetStepAction(step);
                switch (stepAction)
                {
                    case StepAction.Execute:
                        StepStatuses[step] = ExecutionStatus.Running;
                        StepWorkers.Add(StartNewStepWorkerAsync(step));
                        break;

                    case StepAction.Skip:
                        StepStatuses[step] = ExecutionStatus.Failed;
                        try
                        {
                            await UpdateStepAsSkipped(step, "Step was skipped because one or more strict dependencies failed.");
                            Log.Warning("{ExecutionId} {step} Marked step as SKIPPED", Execution.ExecutionId, step);
                        }
                        catch (Exception ex)
                        {
                            Log.Error(ex, "{ExecutionId} {step} Error marking step as SKIPPED", Execution.ExecutionId, step);
                        }
                        break;

                    case StepAction.Wait:
                        break;
                }
            }
        }

        private StepAction GetStepAction(StepExecution step)
        {
            // Step has no dependencies. It can be executed.
            if (!step.ExecutionDependencies.Any())
            {
                return StepAction.Execute;
            }

            var dependencies = step.ExecutionDependencies
                .Select(d => d.DependantOnStepExecution);
            var strictDependencies = step.ExecutionDependencies
                .Where(d => d.StrictDependency)
                .Select(d => d.DependantOnStepExecution);

            // If there are any strict dependencies, which have been marked as failed, skip this step.
            if (strictDependencies.Any(d => StepStatuses.Any(status => status.Value == ExecutionStatus.Failed && status.Key == d)))
            {
                return StepAction.Skip;
            }

            // If the steps dependencies have been completed (success or failure), the step can be executed.
            else if (dependencies.All(dep => StepStatuses[dep] == ExecutionStatus.Success || StepStatuses[dep] == ExecutionStatus.Failed))
            {
                return StepAction.Execute;
            }

            // No action should be taken with this step at this time. Wait until next round.
            return StepAction.Wait;
        }

        private async Task UpdateStepAsSkipped(StepExecution step, string errorMessage)
        {
            using var context = _dbContextFactory.CreateDbContext();
            foreach (var attempt in step.StepExecutionAttempts)
            {
                attempt.ExecutionStatus = StepExecutionStatus.Skipped;
                attempt.StartDateTime = DateTimeOffset.Now;
                attempt.EndDateTime = DateTimeOffset.Now;
                attempt.ErrorMessage = errorMessage;
                context.Attach(attempt).State = EntityState.Modified;
            }
            await context.SaveChangesAsync();
        }

        private enum StepAction
        {
            Execute,
            Skip,
            Wait
        }

    }
}
