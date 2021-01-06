using System.Threading.Tasks;

namespace EtlManagerExecutor
{
    interface IJobExecutor
    {
        Task RunAsync(string executionId, bool notify);
    }
}