using Dapper;
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

        public DependencyModeExecutor(ExecutionConfiguration executionConfiguration) : base(executionConfiguration) { }

        public override async Task RunAsync()
        {
            // List of steps, their dependency steps and whether it's a strict dependency or not.
            Dictionary<Step, HashSet<(Step, bool)>> stepDependencies;
            try
            {
                stepDependencies = await GetStepDependenciesAsync();

                // Find circular step dependencies which are not allowed since they would block each other's executions.
                List<List<Step>> cycles = stepDependencies.ToDictionary(pair => pair.Key, pair => pair.Value.Select(dep => dep.Item1)).FindCycles();

                // If there are circular dependencies, update error message for all steps and cancel execution.
                if (cycles.Count > 0)
                {
                    var encoder = System.Text.Encodings.Web.JavaScriptEncoder.Create(System.Text.Unicode.UnicodeRanges.All);
                    var json = JsonSerializer.Serialize(cycles, new JsonSerializerOptions { WriteIndented = true, Encoder = encoder });
                    await Utility.UpdateErrorMessageAsync(ExecutionConfig, "Execution was cancelled because of circular step dependencies:\n" + json);
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
            var allSteps = await ReadStepsAsync();
            allSteps.ForEach(step =>
            {
                StepStatuses[step] = ExecutionStatus.NotStarted;
                CancellationTokenSources[step.StepId] = new();
            });

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

        private async Task DoRoundAsync(Dictionary<Step, HashSet<(Step, bool)>> stepDependencies)
        {
            var unstartedSteps = StepStatuses.Where(step => step.Value == ExecutionStatus.NotStarted).Select(step => step.Key);
            foreach (var step in unstartedSteps)
            {
                var stepAction = GetStepAction(stepDependencies, step);
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

        private StepAction GetStepAction(Dictionary<Step, HashSet<(Step, bool)>> stepDependencies, Step step)
        {
            // Step has dependencies
            if (stepDependencies.ContainsKey(step))
            {
                HashSet<(Step, bool)> dependencies = stepDependencies[step];
                IEnumerable<Step> strictDependencies = dependencies.Where(dep => dep.Item2).Select(dep => dep.Item1);

                // If there are any strict dependencies, which have been marked as failed, skip this step.
                if (strictDependencies.Any(dep => StepStatuses.Any(status => status.Value == ExecutionStatus.Failed && status.Key == dep)))
                {
                    return StepAction.Skip;
                }

                // If the steps dependencies have been completed (success or failure), the step can be executed.
                // Also check if the dependency is actually included in the execution. If not, the step can be started.
                else if (dependencies.All(dep => !StepStatuses.ContainsKey(dep.Item1) ||
                StepStatuses[dep.Item1] == ExecutionStatus.Success || StepStatuses[dep.Item1] == ExecutionStatus.Failed))
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

        private async Task UpdateStepAsSkipped(Step step)
        {
            using var sqlConnection = new SqlConnection(ExecutionConfig.ConnectionString);
            await sqlConnection.ExecuteAsync(
                @"UPDATE etlmanager.Execution
                SET ExecutionStatus = 'SKIPPED',
                StartDateTime = GETDATE(), EndDateTime = GETDATE()
                WHERE ExecutionId = @ExecutionId AND StepId = @StepId",
                new { ExecutionConfig.ExecutionId, step.StepId });
        }

        private async Task<List<Step>> ReadStepsAsync()
        {
            using var sqlConnection = new SqlConnection(ExecutionConfig.ConnectionString);
            var steps = await sqlConnection.QueryAsync<Step>(
                "SELECT DISTINCT StepId, StepName FROM etlmanager.Execution WHERE ExecutionId = @ExecutionId",
                new { ExecutionConfig.ExecutionId });
            return steps.ToList();
        }

        // Returns a list of steps, its dependency steps and whether it's a strict dependency or not.
        private async Task<Dictionary<Step, HashSet<(Step, bool)>>> GetStepDependenciesAsync()
        {
            using var sqlConnection = new SqlConnection(ExecutionConfig.ConnectionString);
            // Get a list of dependencies for this execution. Only include steps selected for execution in the check (inner join to Execution table).
            var rows = await sqlConnection.QueryAsync<(Guid StepId, string StepName, Guid DependantOnStepId, string DependantOnStepName, bool StrictDependency)>(
                @"SELECT
                    a.StepId,
                    b.StepName,
                    a.DependantOnStepId,
                    c.StepName as DependantOnStepName,
                    a.StrictDependency
                FROM etlmanager.Dependency AS a
                    INNER JOIN etlmanager.Step AS b ON a.StepId = b.StepId
                    INNER JOIN etlmanager.Step AS c ON a.DependantOnStepId = c.StepId
                    INNER JOIN etlmanager.Execution AS d ON a.StepId = d.StepId AND d.ExecutionId = @ExecutionId AND d.RetryAttemptIndex = 0
                    INNER JOIN etlmanager.Execution AS e ON a.DependantOnStepId = e.StepId AND e.ExecutionId = @ExecutionId AND e.RetryAttemptIndex = 0
                WHERE b.JobId = @JobId",
                new { ExecutionConfig.Job.JobId, ExecutionConfig.ExecutionId });
            
            var steps = rows.Select(row => (
            Step: new Step(row.StepId, row.StepName),
            DependantOnStep: new Step(row.DependantOnStepId, row.DependantOnStepName),
            row.StrictDependency)).ToList();

            var dependencies = steps
                .GroupBy(key => key.Step, element => (element.DependantOnStep, element.StrictDependency))
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
