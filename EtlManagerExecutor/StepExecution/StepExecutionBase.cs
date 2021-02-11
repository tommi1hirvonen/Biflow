using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace EtlManagerExecutor
{
    abstract class StepExecutionBase
    {
        protected ExecutionConfiguration Configuration { get; init; }
        protected string StepId { get; init; }
        public int RetryAttemptCounter { get; set; }

        public StepExecutionBase(ExecutionConfiguration configuration, string stepId)
        {
            Configuration = configuration;
            StepId = stepId;
        }

        public abstract Task<ExecutionResult> ExecuteAsync(CancellationToken cancellationToken);
    }
}
