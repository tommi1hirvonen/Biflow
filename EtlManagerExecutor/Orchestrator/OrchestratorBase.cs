using EtlManagerDataAccess.Models;
using EtlManagerUtils;
using Microsoft.Extensions.DependencyInjection;
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
    public abstract class OrchestratorBase
    {
        protected IExecutionConfiguration _executionConfig;
        private readonly IStepExecutorFactory _stepExecutorFactory;

        protected Execution Execution { get; init; }

        private SemaphoreSlim Semaphore { get; init; }

        protected Dictionary<StepExecution, ExtendedCancellationTokenSource> CancellationTokenSources { get; init; }

        protected enum ExecutionStatus
        {
            NotStarted,
            Running,
            Success,
            Failed
        };

        protected Dictionary<StepExecution, ExecutionStatus> StepStatuses { get; init; }

        public OrchestratorBase(IExecutionConfiguration executionConfiguration, IStepExecutorFactory stepExecutorFactory, Execution execution)
        {
            _executionConfig = executionConfiguration;
            _stepExecutorFactory = stepExecutorFactory;
            Execution = execution;

            CancellationTokenSources = Execution.StepExecutions
                .ToDictionary(e => e, _ => new ExtendedCancellationTokenSource());
            StepStatuses = Execution.StepExecutions
                .ToDictionary(e => e, _ => ExecutionStatus.NotStarted);

            // If MaxParallelSteps was defined for the job, use that. Otherwise default to the value from configuration.
            var maxParallelSteps = execution.Job?.MaxParallelSteps ?? 0;
            maxParallelSteps = maxParallelSteps > 0 ? maxParallelSteps : _executionConfig.MaxParallelSteps;
            Semaphore = new SemaphoreSlim(maxParallelSteps, maxParallelSteps);
        }

        public abstract Task RunAsync();

        protected void RegisterCancelListeners()
        {
            // Start listening for cancel key press from the console.
            _ = Task.Run(ReadCancelKey);
            // Start listening for cancel command from the UI application.
            _ = Task.Run(() => ReadCancelPipe(Execution.ExecutionId));
        }

        protected async Task StartNewStepWorkerAsync(StepExecution step)
        {
            // Wait until the semaphore can be entered and the step can be started.
            await Semaphore.WaitAsync();
            // Create a new step worker and start it asynchronously.
            var executor = _stepExecutorFactory.Create(step);
            var task = executor.RunAsync(CancellationTokenSources[step]);
            Log.Information("{ExecutionId} {step} Started step execution", Execution.ExecutionId, step);
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
                Log.Information("{ExecutionId} {step} Finished step execution", Execution.ExecutionId, step);
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
                    ProcessCancelInput(input);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error canceling execution: {ex.Message}");
                }
            } 
        }

        private void ProcessCancelInput(string? input)
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
                var stepId = Guid.Parse(input);
                var step = Execution.StepExecutions.FirstOrDefault(e => e.StepId == stepId);
                if (step is not null && CancellationTokenSources.ContainsKey(step))
                {
                    CancellationTokenSources[step].Cancel("console");
                    Console.WriteLine($"Canceled step {stepId}.");
                }
                else
                {
                    Console.WriteLine("No step running with that step id.");
                }
            }
        }


        private void ReadCancelPipe(Guid executionId)
        {
            while (true)
            {
                using var pipeServer = new NamedPipeServerStream(executionId.ToString().ToLower(), PipeDirection.In);
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
                    if (cancelCommand.StepId is not null)
                    {
                        // Cancel just one step
                        var step = Execution.StepExecutions.FirstOrDefault(e => e.StepId == cancelCommand.StepId);
                        if (step is not null && CancellationTokenSources.ContainsKey(step))
                        {
                            CancellationTokenSources[step].Cancel(cancelCommand.Username);
                        }
                    }
                    else
                    {
                        // Cancel all steps
                        foreach (var pair in CancellationTokenSources)
                        {
                            pair.Value.Cancel(cancelCommand.Username);
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
