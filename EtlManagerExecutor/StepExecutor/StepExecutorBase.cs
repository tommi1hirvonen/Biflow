using System.Threading;
using System.Threading.Tasks;

namespace EtlManagerExecutor
{
    abstract class StepExecutorBase
    {
        protected ExecutionConfiguration Configuration { get; init; }
        public int RetryAttemptCounter { get; set; }

        public StepExecutorBase(ExecutionConfiguration configuration)
        {
            Configuration = configuration;
        }

        public abstract Task<ExecutionResult> ExecuteAsync(CancellationToken cancellationToken);

    }
}
