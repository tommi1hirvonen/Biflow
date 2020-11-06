using System.Threading.Tasks;

namespace EtlManagerExecutor
{
    interface IExecutionStopper
    {
        Task<bool> Run(string executionId, string username, string encryptionKey);
    }
}