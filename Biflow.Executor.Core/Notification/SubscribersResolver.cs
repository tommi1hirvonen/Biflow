namespace Biflow.Executor.Core.Notification;

public class SubscribersResolver(ISubscriptionsProviderFactory subscriptionsProviderFactory) : ISubscribersResolver
{
    private readonly ISubscriptionsProviderFactory _subscriptionsProviderFactory = subscriptionsProviderFactory;

    public async Task<ICollection<string>> ResolveSubscriberEmailsAsync(Execution execution)
    {
        var provider = _subscriptionsProviderFactory.Create(execution);

        var subscribersTask = execution.Notify
            ? GetSubscriberEmailsAsync(provider, execution)
            : Task.FromResult(Enumerable.Empty<string>());
        var subscribers = await subscribersTask;

        var launcherTask = JobSubscriptionShouldAlert(execution, execution.NotifyCaller)
            ? provider.GetLauncherUserAsync()
            : Task.FromResult(null as User);
        var launcher = await launcherTask;

        var allSubscribers = subscribers
            .Append(launcher?.Email ?? "")
            .Distinct()
            .Where(s => !string.IsNullOrEmpty(s))
            .ToArray();

        return allSubscribers;
    }

    public async Task<ICollection<string>> ResolveLongRunningSubscriberEmailsAsync(Execution execution)
    {
        var provider = _subscriptionsProviderFactory.Create(execution);

        var subscriptionsTask = execution.Notify
            ? provider.GetJobSubscriptionsAsync()
            : Task.FromResult(Enumerable.Empty<JobSubscription>());
        var subscriptions = await subscriptionsTask;
        var subscribers = subscriptions
            .Where(s => s.NotifyOnOvertime)
            .Select(s => s.User.Email ?? "")
            .Distinct();

        var launcherTask = execution.NotifyCallerOvertime
            ? provider.GetLauncherUserAsync()
            : Task.FromResult(null as User);
        var launcher = await launcherTask;

        var allSubscribers = subscribers
            .Append(launcher?.Email ?? "")
            .Distinct()
            .Where(s => !string.IsNullOrEmpty(s))
            .ToArray();

        return allSubscribers;
    }

    private static async Task<IEnumerable<string>> GetSubscriberEmailsAsync(ISubscriptionsProvider provider, Execution execution)
    {
        // Map tags to steps for tag based subscriptions
        var tagSteps = await provider.GetTagStepsAsync();

        var jobSubscriptions = await provider.GetJobSubscriptionsAsync();
        var stepSubscriptions = await provider.GetStepSubscriptionsAsync();
        var stepTagSubscriptions = await provider.GetStepTagSubscriptionsAsync();
        var jobStepTagSubscriptions = await provider.GetJobStepTagSubscriptionsAsync();

        var jobSubscribers = jobSubscriptions
            .Where(s => JobSubscriptionShouldAlert(execution, s.AlertType))
            .Select(s => s.User.Email ?? "");
        var stepSubscribers = stepSubscriptions
            .Where(s => StepSubscriptionShouldAlert(s.AlertType, s.StepId))
            .Select(s => s.User.Email ?? "");
        var stepTagSubscribers = stepTagSubscriptions
            .Where(s => TagSubscriptionShouldAlert(s.AlertType, s.TagId))
            .Select(s => s.User.Email ?? "");
        var jobStepTagSubscribers = jobStepTagSubscriptions
            .Where(s => TagSubscriptionShouldAlert(s.AlertType, s.TagId))
            .Select(s => s.User.Email ?? "");

        return jobSubscribers
            .Concat(stepSubscribers)
            .Concat(stepTagSubscribers)
            .Concat(jobStepTagSubscribers);

        bool StepSubscriptionShouldAlert(AlertType alert, Guid stepId) => (alert, execution.StepExecutions.FirstOrDefault(s => s.StepId == stepId)?.ExecutionStatus) switch
        {
            (_, null) => false,
            (AlertType.OnCompletion, _) => true,
            (AlertType.OnFailure, StepExecutionStatus.Failed or StepExecutionStatus.Stopped or StepExecutionStatus.DependenciesFailed or StepExecutionStatus.Duplicate) => true,
            (AlertType.OnSuccess, StepExecutionStatus.Succeeded or StepExecutionStatus.Warning) => true,
            _ => false
        };
        
        bool TagSubscriptionShouldAlert(AlertType alert, Guid tagId) => tagSteps.TryGetValue(tagId, out var stepIds) switch
        {
            true => stepIds.Any(id => StepSubscriptionShouldAlert(alert, id)),
            false => false
        };
    }

    private static bool JobSubscriptionShouldAlert(Execution execution, AlertType? alert) => (alert, execution.ExecutionStatus) switch
    {
        (AlertType.OnCompletion, _) => true,
        (AlertType.OnFailure, ExecutionStatus.Failed or ExecutionStatus.Stopped or ExecutionStatus.Suspended or ExecutionStatus.NotStarted or ExecutionStatus.Running) => true,
        (AlertType.OnSuccess, ExecutionStatus.Succeeded or ExecutionStatus.Warning) => true,
        _ => false
    };
}

public interface ISubscribersResolver
{
    public Task<ICollection<string>> ResolveLongRunningSubscriberEmailsAsync(Execution execution);
    public Task<ICollection<string>> ResolveSubscriberEmailsAsync(Execution execution);
}