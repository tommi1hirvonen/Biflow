using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace EtlManagerExecutor
{
    abstract class ExecutorBase
    {
        public record CancelCommand(string StepId, string Username);

        protected ExecutionConfiguration ExecutionConfig { get; init; }

        private SemaphoreSlim Semaphore { get; init; }

        protected Dictionary<string, CancellationTokenSource> CancellationTokenSources { get; } = new();

        protected enum ExecutionStatus
        {
            NotStarted,
            Running,
            Success,
            Failed
        };

        protected Dictionary<Step, ExecutionStatus> StepStatuses { get; } = new();

        public ExecutorBase(ExecutionConfiguration executionConfiguration)
        {
            ExecutionConfig = executionConfiguration;
            Semaphore = new SemaphoreSlim(ExecutionConfig.MaxParallelSteps, ExecutionConfig.MaxParallelSteps);
        }

        public abstract Task RunAsync();

        protected void RegisterCancelListeners()
        {
            // Start listening for cancel key press from the console.
            _ = Task.Run(ReadCancelKey);
            // Start listening for cancel command from the UI application.
            _ = Task.Run(() => ReadCancelPipe(ExecutionConfig.ExecutionId));
        }

        protected async Task StartNewStepWorkerAsync(Step step)
        {
            // Wait until the semaphore can be entered and the step can be started.
            await Semaphore.WaitAsync();
            // Create a new step worker and start it asynchronously.
            var token = CancellationTokenSources[step.StepId].Token;
            var task = new StepWorker(ExecutionConfig, step).ExecuteStepAsync(token);
            Log.Information("{ExecutionId} {step} Started step execution", ExecutionConfig.ExecutionId, step);
            bool result = false;
            try
            {
                // Wait for the step to finish.
                result = await task;
            }
            finally
            {
                // Update the status.
                StepStatuses[step] = result ? ExecutionStatus.Success : ExecutionStatus.Failed;
                // Release the semaphore once to make room for new parallel executions.
                Semaphore.Release();
                Log.Information("{ExecutionId} {step} Finished step execution", ExecutionConfig.ExecutionId, step);
            }
        }

        private void ReadCancelKey()
        {
            Console.WriteLine("Enter 'c' to cancel all step executions or a step id to cancel that step's execution.");
            while (true)
            {
                var input = Console.ReadLine();
                try
                {
                    if (input == "c")
                    {
                        Console.WriteLine("Canceling all step executions.");
                        foreach (var pair in CancellationTokenSources)
                        {
                            pair.Value.Cancel();
                        }
                    }
                    else if (input is not null)
                    {
                        if (CancellationTokenSources.ContainsKey(input))
                        {
                            CancellationTokenSources[input].Cancel();
                            Console.WriteLine($"Canceled step {input}.");
                        }
                        else
                        {
                            Console.WriteLine("No step running with that step id.");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error canceling execution: {ex.Message}");
                }
            } 
        }

        private void ReadCancelPipe(string executionId)
        {
            while (true)
            {
                using var pipeServer = new NamedPipeServerStream(executionId.ToLower(), PipeDirection.In);
                pipeServer.WaitForConnection();
                try
                {
                    using var streamReader = new StreamReader(pipeServer);
                    var builder = new StringBuilder();
                    string? input;
                    while ((input = streamReader.ReadLine()) is not null)
                    {
                        builder.Append(input);
                    }
                    var json = builder.ToString();
                    var cancelCommand = JsonSerializer.Deserialize<CancelCommand>(json)
                        ?? throw new ArgumentNullException("cancelCommand", "Cancel command cannot be null");
                    // Change the user to the one initiated the cancel.
                    ExecutionConfig.Username = cancelCommand.Username;
                    if (cancelCommand.StepId is not null)
                    {
                        // Cancel just one step
                        CancellationTokenSources[cancelCommand.StepId].Cancel();
                    }
                    else
                    {
                        // Cancel all steps
                        foreach (var pair in CancellationTokenSources)
                        {
                            pair.Value.Cancel();
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Error canceling execution");
                }
            }
            
        }
    }
}
