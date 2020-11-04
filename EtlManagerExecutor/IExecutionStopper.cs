using System.Threading.Tasks;

namespace EtlManagerExecutor
{
    interface IExecutionStopper
    {
        Task Run(string executionId, string username);
    }
}