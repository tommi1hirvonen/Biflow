using Biflow.DataAccess;
using Biflow.DataAccess.Models;
using Microsoft.EntityFrameworkCore;

namespace Biflow.Ui.Core
{
    public class SubscriptionsHelperService
    {

        private readonly IDbContextFactory<BiflowContext> _dbContextFactory;

        public SubscriptionsHelperService(IDbContextFactory<BiflowContext> dbContextFactory)
        {
            _dbContextFactory = dbContextFactory;
        }

        public async Task RemoveSubscription(User user, Job job)
        {
            var subscription = user.Subscriptions.FirstOrDefault(sub => sub.JobId == job.JobId);
            if (subscription is null)
                return;

            using var context = _dbContextFactory.CreateDbContext();
            context.Subscriptions.Remove(subscription);
            user.Subscriptions.Remove(subscription);
            await context.SaveChangesAsync();
        }

        public async Task ToggleSubscription(User user, Job job, SubscriptionType subscriptionType)
        {
            using var context = _dbContextFactory.CreateDbContext();
            var subscription = user.Subscriptions.FirstOrDefault(sub => sub.JobId == job.JobId);
            if (subscription is not null)
            {
                subscription.SubscriptionType = subscriptionType;
                context.Attach(subscription).State = EntityState.Modified;
            }
            else
            {
                var newSub = new Subscription(job.JobId, user.Username) { SubscriptionType = subscriptionType };
                context.Subscriptions.Add(newSub);
                user.Subscriptions.Add(newSub);
            }
            await context.SaveChangesAsync();
        }

        public async Task ToggleSubscription(User user, Job job, bool onOvertime)
        {
            using var context = _dbContextFactory.CreateDbContext();
            var subscription = user.Subscriptions.FirstOrDefault(sub => sub.JobId == job.JobId);
            if (subscription is not null && subscription.SubscriptionType is null && !onOvertime)
            {
                context.Subscriptions.Remove(subscription);
                user.Subscriptions.Remove(subscription);
            }
            else if (subscription is not null)
            {
                subscription.NotifyOnOvertime = onOvertime;
                context.Attach(subscription).State = EntityState.Modified;
            }
            else
            {
                var newSub = new Subscription(job.JobId, user.Username) { SubscriptionType = null, NotifyOnOvertime = onOvertime };
                context.Subscriptions.Add(newSub);
                user.Subscriptions.Add(newSub);
            }
            await context.SaveChangesAsync();
        }

    }
}
