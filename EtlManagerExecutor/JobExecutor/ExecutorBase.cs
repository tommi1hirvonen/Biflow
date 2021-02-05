using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace EtlManagerExecutor
{
    abstract class ExecutorBase
    {
        protected ExecutionConfiguration ExecutionConfig { get; init; }
        protected SemaphoreSlim Semaphore { get; init; }
        protected CancellationTokenSource CancellationTokenSource { get; } = new();

        public ExecutorBase(ExecutionConfiguration executionConfiguration)
        {
            ExecutionConfig = executionConfiguration;
            Semaphore = new SemaphoreSlim(ExecutionConfig.MaxParallelSteps, ExecutionConfig.MaxParallelSteps);
        }

        public abstract Task RunAsync();

        protected void ReadCancelKey()
        {
            Console.WriteLine("Press c to cancel execution");
            ConsoleKeyInfo cki;
            do
            {
                cki = Console.ReadKey();
            } while (cki.KeyChar != 'c');

            Console.WriteLine("Canceling all step executions");
            CancellationTokenSource.Cancel();
        }

        protected void ReadCancelPipe(string executionId)
        {
            using var pipeServer = new NamedPipeServerStream(executionId.ToLower(), PipeDirection.In);
            pipeServer.WaitForConnection();
            using var streamReader = new StreamReader(pipeServer);
            string input;
            if ((input = streamReader.ReadLine()) is not null)
            {
                // Change the user to the one initiated the cancel.
                //The UI application provides the username as the pipe input.
                ExecutionConfig.Username = input;
                CancellationTokenSource.Cancel();
            }
        }
    }
}
