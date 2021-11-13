using EtlManager.DataAccess.Models;

namespace EtlManager.Executor;

interface IJobExecutor
{
    Task RunAsync(Guid executionId, bool notify, SubscriptionType? notifyMe, bool notifyMeOvertime);
}
