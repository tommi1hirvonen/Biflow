using System.Threading.Tasks;

namespace EtlManagerExecutor;

interface IEmailTest
{
    public Task RunAsync(string toAddress);
}
