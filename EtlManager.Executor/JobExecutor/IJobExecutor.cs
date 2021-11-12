using System;
using System.Threading.Tasks;

namespace EtlManager.Executor;

interface IJobExecutor
{
    Task RunAsync(Guid executionId, bool notify);
}
