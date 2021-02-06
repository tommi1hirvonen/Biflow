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
        private record Step(string StepId, string StepName);

        private List<Task> StepWorkers { get; } = new();
        enum ExecutionStatus
        {
            NotStarted,
            Running,
            Success,
            Failed
        };

        private Dictionary<string, ExecutionStatus> StepStatuses { get; set; } = new();


        public DependencyModeExecutor(ExecutionConfiguration executionConfiguration) : base(executionConfiguration) { }

        public override async Task RunAsync()
        {
            // List of steps (id), their dependency steps (id) and whether it's a strict dependency or not.
            Dictionary<string, HashSet<KeyValuePair<string, bool>>> stepDependencies;
            try
            {
                Dictionary<Step, List<KeyValuePair<Step, bool>>> dependencies = await GetStepDependenciesAsync();
                stepDependencies = dependencies.ToDictionary(
                    pair => pair.Key.StepId,
                    pair => pair.Value.Select(dependency => new KeyValuePair<string, bool>(dependency.Key.StepId, dependency.Value)
                    ).ToHashSet());

                // Find circular step dependencies which are not allowed since they would block each other's executions.
                List<List<Step>> cycles = dependencies.ToDictionary(pair => pair.Key, pair => pair.Value.Select(dep => dep.Key)).FindCycles();

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
            using (var sqlConnection = new SqlConnection(ExecutionConfig.ConnectionString))
            {
                await sqlConnection.OpenAsync();

                // Get steps to execute and add them to the list of steps and their statuses.
                var stepsListCommand = new SqlCommand("SELECT DISTINCT StepId FROM etlmanager.Execution WHERE ExecutionId = @ExecutionId", sqlConnection);
                stepsListCommand.Parameters.AddWithValue("@ExecutionId", ExecutionConfig.ExecutionId);
                using var reader = await stepsListCommand.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    var stepId = reader[0].ToString();
                    StepStatuses[stepId] = ExecutionStatus.NotStarted;
                    CancellationTokenSources[stepId] = new();
                }
            }

            // Start listening for cancel key press from the console.
            _ = Task.Run(ReadCancelKey);
            // Start listening for cancel command from the UI application.
            _ = Task.Run(() => ReadCancelPipe(ExecutionConfig.ExecutionId));

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

        private async Task DoRoundAsync(Dictionary<string, HashSet<KeyValuePair<string, bool>>> stepDependencies)
        {
            IEnumerable<string> stepsToExecute = StepStatuses.Where(step => step.Value == ExecutionStatus.NotStarted).Select(step => step.Key);
            var stepsToSkip = new List<string>();
            var executableSteps = new List<string>();

            foreach (var stepId in stepsToExecute)
            {
                // Step has dependencies
                if (stepDependencies.ContainsKey(stepId))
                {
                    HashSet<KeyValuePair<string, bool>> dependencies = stepDependencies[stepId];
                    IEnumerable<string> strictDependencies = dependencies.Where(dep => dep.Value).Select(dep => dep.Key);

                    // If there are any strict dependencies, which have been marked as failed, skip this step.
                    if (strictDependencies.Any(dep => StepStatuses.Any(status => status.Value == ExecutionStatus.Failed && status.Key == dep)))
                    {
                        stepsToSkip.Add(stepId);
                    }
                    // If the steps dependencies have been completed (success or failure), the step can be executed.
                    // Also check if the dependency is actually included in the execution. If not, the step can be started.
                    else if (dependencies.All(dep => !StepStatuses.ContainsKey(dep.Key) ||
                    StepStatuses[dep.Key] == ExecutionStatus.Success || StepStatuses[dep.Key] == ExecutionStatus.Failed))
                    {
                        executableSteps.Add(stepId);
                    }
                    // No action should be taken with this step at this time. Wait until next round.
                }
                else
                {
                    // Step has no dependencies. It can be executed.
                    executableSteps.Add(stepId);
                }
            }

            // Mark the steps that should be skipped in the execution table as SKIPPED.
            foreach (string stepId in stepsToSkip)
            {
                StepStatuses[stepId] = ExecutionStatus.Failed;
                try
                {
                    using var sqlConnection = new SqlConnection(ExecutionConfig.ConnectionString);
                    await sqlConnection.OpenAsync();
                    var skipUpdateCommand = new SqlCommand(
                        @"UPDATE etlmanager.Execution
                        SET ExecutionStatus = 'SKIPPED',
                        StartDateTime = GETDATE(), EndDateTime = GETDATE()
                        WHERE ExecutionId = @ExecutionId AND StepId = @StepId"
                        , sqlConnection)
                    {
                        CommandTimeout = 120 // two minutes
                    };
                    skipUpdateCommand.Parameters.AddWithValue("@ExecutionId", ExecutionConfig.ExecutionId);
                    skipUpdateCommand.Parameters.AddWithValue("@StepId", stepId);
                    await skipUpdateCommand.ExecuteNonQueryAsync();
                    Log.Warning("{ExecutionId} {stepId} Marked step as SKIPPED", ExecutionConfig.ExecutionId, stepId);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "{ExecutionId} {stepId} Error marking step as SKIPPED", ExecutionConfig.ExecutionId, stepId);
                }
            }

            foreach (string stepId in executableSteps)
            {
                StepStatuses[stepId] = ExecutionStatus.Running;
                StepWorkers.Add(StartNewStepWorkerAsync(stepId));
            }
        }

        private async Task StartNewStepWorkerAsync(string stepId)
        {
            // Wait until the semaphore can be entered and the step can be started.
            await Semaphore.WaitAsync();
            // Create a new step worker and start it asynchronously.
            var task = new StepWorker(ExecutionConfig, stepId).ExecuteStepAsync(CancellationTokenSources[stepId].Token);
            Log.Information("{ExecutionId} {stepId} Started step execution", ExecutionConfig.ExecutionId, stepId);
            bool result = false;
            try
            {
                // Wait for the step to finish.
                result = await task;
            }
            finally
            {
                // Update the status.
                StepStatuses[stepId] = result ? ExecutionStatus.Success : ExecutionStatus.Failed;
                // Release the semaphore once to make room for new parallel executions.
                Semaphore.Release();
                Log.Information("{ExecutionId} {StepId} Finished step execution", ExecutionConfig.ExecutionId, stepId);
            }
        }

        // Returns a list of steps, its dependency steps and whether it's a strict dependency or not.
        private async Task<Dictionary<Step, List<KeyValuePair<Step, bool>>>> GetStepDependenciesAsync()
        {
            using var sqlConnection = new SqlConnection(ExecutionConfig.ConnectionString);
            // Get a list of dependencies for this execution. Only include steps selected for execution in the check (inner join to Execution table).
            var sqlCommand = new SqlCommand(
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
                    WHERE b.JobId = @JobId"
                    , sqlConnection)
            {
                CommandTimeout = 120 // two minutes
            };
            sqlCommand.Parameters.AddWithValue("@JobId", ExecutionConfig.JobId);
            sqlCommand.Parameters.AddWithValue("@ExecutionId", ExecutionConfig.ExecutionId);
            var dependencies = new Dictionary<Step, List<KeyValuePair<Step, bool>>>();
            await sqlConnection.OpenAsync();
            using (var reader = await sqlCommand.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    var stepId = reader["StepId"].ToString();
                    var stepName = reader["StepName"].ToString();
                    var dependencyStepId = reader["DependantOnStepId"].ToString();
                    var dependencyStepName = reader["DependantOnStepName"].ToString();
                    var strict = (bool)reader["StrictDependency"];

                    var step = new Step(stepId, stepName);
                    var dependencyStep = new Step(dependencyStepId, dependencyStepName);

                    if (!dependencies.ContainsKey(step))
                        dependencies[step] = new();

                    dependencies[step].Add(new(dependencyStep, strict));
                }
            }

            return dependencies;
        }

    }
}
