using System.Threading.Tasks;

namespace EtlManagerExecutor
{
    interface IMailTest
    {
        public Task RunAsync(string toAddress);
    }
}