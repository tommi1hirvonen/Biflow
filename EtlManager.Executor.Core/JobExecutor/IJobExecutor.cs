using EtlManager.DataAccess.Models;
using EtlManager.Executor.Core.Orchestrator;

namespace EtlManager.Executor.Core.JobExecutor;

public interface IJobExecutor
{
    public void Cancel(string username);

    public void Cancel(string username, Guid stepId);

    Task RunAsync(Guid executionId, bool notify, SubscriptionType? notifyMe, bool notifyMeOvertime);
}
