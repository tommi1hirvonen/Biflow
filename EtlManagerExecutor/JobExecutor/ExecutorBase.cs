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
        protected SemaphoreSlim Semaphore { get; init; }
        protected Dictionary<string, CancellationTokenSource> CancellationTokenSources { get; } = new();

        public ExecutorBase(ExecutionConfiguration executionConfiguration)
        {
            ExecutionConfig = executionConfiguration;
            Semaphore = new SemaphoreSlim(ExecutionConfig.MaxParallelSteps, ExecutionConfig.MaxParallelSteps);
        }

        public abstract Task RunAsync();

        protected void ReadCancelKey()
        {
            Console.WriteLine("Enter 'c' to cancel all step executions or a step id to cancel that step's execution.");
            while (true)
            {
                string input = Console.ReadLine();
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
                    else
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

        protected void ReadCancelPipe(string executionId)
        {
            while (true)
            {
                using var pipeServer = new NamedPipeServerStream(executionId.ToLower(), PipeDirection.In);
                pipeServer.WaitForConnection();
                try
                {
                    using var streamReader = new StreamReader(pipeServer);
                    var builder = new StringBuilder();
                    string input;
                    while ((input = streamReader.ReadLine()) is not null)
                    {
                        builder.Append(input);
                    }
                    var json = builder.ToString();
                    var cancelCommand = JsonSerializer.Deserialize<CancelCommand>(json);
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
