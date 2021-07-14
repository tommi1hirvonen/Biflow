using Dapper;
using EtlManagerDataAccess.Models;
using Microsoft.EntityFrameworkCore;
using Serilog;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace EtlManagerExecutor
{
    class DependencyModeExecutor : ExecutorBase
    {
        private List<Task> StepWorkers { get; } = new();

        private Dictionary<Guid, StepExecution> StepExecutions { get; } = new();

        public DependencyModeExecutor(ExecutionConfiguration executionConfiguration, Execution execution)
            : base(executionConfiguration, execution) { }

        public override async Task RunAsync()
        {
            // List of steps, their dependency steps and whether it's a strict dependency or not.
            Dictionary<Step, HashSet<(Step Step, bool StrictDependency)>> stepDependencies;
            try
            {
                stepDependencies = await GetStepDependenciesAsync();

                // Find circular step dependencies which are not allowed since they would block each other's executions.
                List<List<Step>> cycles = stepDependencies.ToDictionary(pair => pair.Key, pair => pair.Value.Select(dep => dep.Step)).FindCycles();

                // If there are circular dependencies, update error message for all steps and cancel execution.
                if (cycles.Count > 0)
                {
                    var steps = cycles.Select(c => c.Select(c_ => new { c_.StepId, c_.StepName }).ToList()).ToList();
                    var encoder = System.Text.Encodings.Web.JavaScriptEncoder.Create(System.Text.Unicode.UnicodeRanges.All);
                    var json = JsonSerializer.Serialize(steps, new JsonSerializerOptions { WriteIndented = true, Encoder = encoder });
                    var errorMessage = "Execution was cancelled because of circular step dependencies:\n" + json;

                    using var context = ExecutionConfig.DbContextFactory.CreateDbContext();
                    foreach (var attempt in Execution.StepExecutions.SelectMany(e => e.StepExecutionAttempts))
                    {
                        attempt.StartDateTime = DateTime.Now;
                        attempt.EndDateTime = DateTime.Now;
                        attempt.ErrorMessage = errorMessage;
                        attempt.ExecutionStatus = StepExecutionStatus.Failed;
                        context.Attach(attempt).State = EntityState.Modified;
                    }
                    await context.SaveChangesAsync();

                    Log.Error("{ExecutionId} Execution was cancelled because of circular step dependencies: " + json, ExecutionConfig.ExecutionId);
                    return;
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "{ExecutionId} Error checking for possible circular step dependencies", ExecutionConfig.ExecutionId);
                return;
            }


            // Populate StepStatuses with a list of steps to execute.
            foreach (var step in Execution.StepExecutions)
            {
                StepStatuses[step.StepId] = ExecutionStatus.NotStarted;
                CancellationTokenSources[step.StepId] = new();
                StepExecutions[step.StepId] = step;
            }

            // Start listening for cancel commands.
            RegisterCancelListeners();

            // Loop as long as there are steps that haven't yet been started.
            while (StepStatuses.Any(step => step.Value == ExecutionStatus.NotStarted))
            {
                // Start steps that can be started and skip those that should be skipped.
                await DoRoundAsync(stepDependencies);
                // Wait for at least one step to finish before doing another round.
                await Task.WhenAny(StepWorkers);
                // Remove finished tasks from the list so that they don't immediately trigger the next Task.WhenAny().
                StepWorkers.RemoveAll(task => task.IsCompleted);
            }

            // All steps have been started. Wait until the remaining step worker tasks have finished.
            await Task.WhenAll(StepWorkers);
        }

        private async Task DoRoundAsync(Dictionary<Step, HashSet<(Step Step, bool StrictDependency)>> stepDependencies)
        {
            var unstartedStepIds = StepStatuses
                .Where(status => status.Value == ExecutionStatus.NotStarted)
                .Select(status => status.Key);
            foreach (var stepId in unstartedStepIds)
            {
                var step = StepExecutions[stepId];
                var stepAction = GetStepAction(stepDependencies, step);
                switch (stepAction)
                {
                    case StepAction.Execute:
                        StepStatuses[stepId] = ExecutionStatus.Running;
                        StepWorkers.Add(StartNewStepWorkerAsync(step));
                        break;

                    case StepAction.Skip:
                        StepStatuses[stepId] = ExecutionStatus.Failed;
                        try
                        {
                            await UpdateStepAsSkipped(step);
                            Log.Warning("{ExecutionId} {step} Marked step as SKIPPED", ExecutionConfig.ExecutionId, step);
                        }
                        catch (Exception ex)
                        {
                            Log.Error(ex, "{ExecutionId} {step} Error marking step as SKIPPED", ExecutionConfig.ExecutionId, step);
                        }
                        break;

                    case StepAction.Wait:
                        break;
                }
            }
        }

        private StepAction GetStepAction(Dictionary<Step, HashSet<(Step Step, bool StrictDependency)>> stepDependencies, StepExecution stepExecution)
        {
            // Get the step corresponding the step execution.
            var step = stepDependencies.Keys.FirstOrDefault(step => step.StepId == stepExecution.StepId);
            // Step has dependencies
            if (step is not null)
            {
                HashSet<(Step Step, bool StrictDependency)> dependencies = stepDependencies[step];
                IEnumerable<Step> strictDependencies = dependencies.Where(dep => dep.StrictDependency).Select(dep => dep.Step);

                // If there are any strict dependencies, which have been marked as failed, skip this step.
                if (strictDependencies.Any(dep => StepStatuses.Any(status => status.Value == ExecutionStatus.Failed && status.Key == dep.StepId)))
                {
                    return StepAction.Skip;
                }

                // If the steps dependencies have been completed (success or failure), the step can be executed.
                // Also check if the dependency is actually included in the execution. If not, the step can be started.
                else if (dependencies.All(dep => !StepStatuses.ContainsKey(dep.Step.StepId) ||
                StepStatuses[dep.Step.StepId] == ExecutionStatus.Success || StepStatuses[dep.Step.StepId] == ExecutionStatus.Failed))
                {
                    return StepAction.Execute;
                }

                // No action should be taken with this step at this time. Wait until next round.
                return StepAction.Wait;
            }
            else
            {
                // Step has no dependencies. It can be executed.
                return StepAction.Execute;
            }
        }

        private async Task UpdateStepAsSkipped(StepExecution step)
        {
            using var context = ExecutionConfig.DbContextFactory.CreateDbContext();
            foreach (var attempt in step.StepExecutionAttempts)
            {
                attempt.ExecutionStatus = StepExecutionStatus.Skipped;
                attempt.StartDateTime = DateTime.Now;
                attempt.EndDateTime = DateTime.Now;
                context.Attach(attempt).State = EntityState.Modified;
            }
            await context.SaveChangesAsync();
        }

        // Returns a list of steps, its dependency steps and whether it's a strict dependency or not.
        private async Task<Dictionary<Step, HashSet<(Step Step, bool StrictDependency)>>> GetStepDependenciesAsync()
        {
            using var context = ExecutionConfig.DbContextFactory.CreateDbContext();
            var steps = await context.Dependencies
                .AsNoTrackingWithIdentityResolution()
                .Include(dep => dep.Step)
                .Include(dep => dep.DependantOnStep)
                .Where(dep => context.StepExecutions.Any(e => e.ExecutionId == ExecutionConfig.ExecutionId && e.StepId == dep.StepId))
                .Where(dep => context.StepExecutions.Any(e => e.ExecutionId == ExecutionConfig.ExecutionId && e.StepId == dep.DependantOnStepId))
                .ToListAsync();
            var dependencies = steps.GroupBy(key => key.Step, element => (element.DependantOnStep, element.StrictDependency))
                .ToDictionary(grouping => grouping.Key, grouping => grouping.ToHashSet());
            return dependencies;
        }

        private enum StepAction
        {
            Execute,
            Skip,
            Wait
        }

    }
}
