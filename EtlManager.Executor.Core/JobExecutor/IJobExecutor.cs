using EtlManager.DataAccess.Models;

namespace EtlManager.Executor.Core.JobExecutor;

public interface IJobExecutor
{
    Task RunAsync(Guid executionId, bool notify, SubscriptionType? notifyMe, bool notifyMeOvertime);
}
