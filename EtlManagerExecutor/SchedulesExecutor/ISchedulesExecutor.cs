using System.Threading.Tasks;

namespace EtlManagerExecutor
{
    interface ISchedulesExecutor
    {
        public Task RunAsync(int hours, int minutes);
    }
}