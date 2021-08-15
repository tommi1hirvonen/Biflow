using System.Threading;
using System.Threading.Tasks;

namespace EtlManagerExecutor
{
    public interface IStepExecutor
    {
        public int RetryAttemptCounter { get; set; }

        public Task<ExecutionResult> ExecuteAsync(ExtendedCancellationTokenSource cancellationTokenSource);

    }
}
