using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace EtlManagerExecutor
{
    abstract class StepExecutionBase
    {
        protected ExecutionConfiguration Configuration { get; init; }
        protected Step Step { get; init; }
        public int RetryAttemptCounter { get; set; }

        public StepExecutionBase(ExecutionConfiguration configuration, Step step)
        {
            Configuration = configuration;
            Step = step;
        }

        public abstract Task<ExecutionResult> ExecuteAsync(CancellationToken cancellationToken);

    }
}
