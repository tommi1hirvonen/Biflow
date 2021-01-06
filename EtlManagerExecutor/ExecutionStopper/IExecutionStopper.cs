using System.Threading.Tasks;

namespace EtlManagerExecutor
{
    interface IExecutionStopper
    {
        Task<bool> RunAsync(string executionId, string username);
    }
}