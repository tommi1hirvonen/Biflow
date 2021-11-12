using System.Threading.Tasks;

namespace EtlManager.Executor;

interface IEmailTest
{
    public Task RunAsync(string toAddress);
}
