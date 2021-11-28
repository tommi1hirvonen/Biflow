using EtlManager.DataAccess.Models;

namespace EtlManager.Utilities;

public class StartCommand
{
    public Guid ExecutionId { get; set; }
    public bool Notify { get; set; }
    public SubscriptionType? NotifyMe { get; set; }
    public bool NotifyMeOvertime { get; set; }
}
