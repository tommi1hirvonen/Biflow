using System;
using System.Threading.Tasks;

namespace EtlManagerExecutor
{
    interface IJobExecutor
    {
        Task RunAsync(Guid executionId, bool notify);
    }
}