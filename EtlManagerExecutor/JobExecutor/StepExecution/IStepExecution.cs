using System.Threading.Tasks;

namespace EtlManagerExecutor
{
    interface IStepExecution
    {
        public Task<ExecutionResult> RunAsync();
    }
}
