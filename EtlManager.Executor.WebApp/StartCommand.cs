using EtlManager.DataAccess.Models;

namespace EtlManager.Executor.WebApp;

public class StartCommand
{
    public Guid ExecutionId { get; set; }
    public bool Notify { get; set; }
    public SubscriptionType? NotifyMe { get; set; }
    public bool NotifyMeOvertime { get; set; }
}
