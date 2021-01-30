using System;
using System.Collections.Generic;
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

        public ExecutorBase(ExecutionConfiguration executionConfiguration)
        {
            ExecutionConfig = executionConfiguration;
            Semaphore = new SemaphoreSlim(ExecutionConfig.MaxParallelSteps, ExecutionConfig.MaxParallelSteps);
        }

        public abstract Task RunAsync();
    }
}
