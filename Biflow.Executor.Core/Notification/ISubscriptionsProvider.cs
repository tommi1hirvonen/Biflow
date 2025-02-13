namespace Biflow.Executor.Core.Notification;

public interface ISubscriptionsProvider
{
    public Task<IEnumerable<JobSubscription>> GetJobSubscriptionsAsync();
    public Task<IEnumerable<JobStepTagSubscription>> GetJobTagSubscriptionsAsync();
    public Task<User?> GetLauncherUserAsync();
    public Task<IEnumerable<StepSubscription>> GetStepSubscriptionsAsync();
    public Task<IDictionary<Guid, IEnumerable<Guid>>> GetTagStepsAsync();
    public Task<IEnumerable<StepTagSubscription>> GetTagSubscriptionsAsync();
}