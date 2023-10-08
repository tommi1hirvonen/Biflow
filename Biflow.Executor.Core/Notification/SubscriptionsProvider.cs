using Biflow.DataAccess.Models;
using Microsoft.EntityFrameworkCore;

namespace Biflow.Executor.Core.Notification;

internal class SubscriptionsProvider(IDbContextFactory<ExecutorDbContext> dbContextFactory, Execution execution) : ISubscriptionsProvider
{
    private readonly ExecutorDbContext _dbContext = dbContextFactory.CreateDbContext();
    private readonly Execution _execution = execution;

    public async Task<IEnumerable<JobSubscription>> GetJobSubscriptionsAsync() => await _dbContext.JobSubscriptions
        .AsNoTracking()
        .Include(s => s.User)
        .Where(s => s.User.Email != null && s.JobId == _execution.JobId)
        .ToArrayAsync();

    public async Task<IEnumerable<StepSubscription>> GetStepSubscriptionsAsync() => await _dbContext.StepSubscriptions
        .AsNoTracking()
        .Include(s => s.User)
        .Where(s => s.User.Email != null &&
                    _dbContext.StepExecutions.Any(e => e.ExecutionId == _execution.ExecutionId && e.StepId == s.StepId))
        .ToArrayAsync();

    public async Task<IEnumerable<TagSubscription>> GetTagSubscriptionsAsync() => await _dbContext.TagSubscriptions
        .AsNoTracking()
        .Include(s => s.User)
        .Where(s => s.User.Email != null &&
                    _dbContext.StepExecutions.Any(e => e.ExecutionId == _execution.ExecutionId && e.Step!.Tags.Any(t => t.TagId == s.TagId)))
        .ToArrayAsync();

    public async Task<IEnumerable<JobTagSubscription>> GetJobTagSubscriptionsAsync() => await _dbContext.JobTagSubscriptions
        .AsNoTracking()
        .Include(s => s.User)
        .Where(s => s.User.Email != null &&
                    s.JobId == _execution.JobId &&
                    _dbContext.StepExecutions.Any(e => e.ExecutionId == _execution.ExecutionId && e.Step!.Tags.Any(t => t.TagId == s.TagId)))
        .ToArrayAsync();

    public async Task<User?> GetLauncherUserAsync() =>
        await _dbContext.Users.FirstOrDefaultAsync(u => u.Username == _execution.CreatedBy);

    public async Task<IDictionary<Guid, IEnumerable<Guid>>> GetTagStepsAsync()
    {
        var stepTags = await _dbContext.Steps
            .Where(s => _dbContext.StepExecutions.Any(e => e.ExecutionId == _execution.ExecutionId && e.StepId == s.StepId))
            .SelectMany(s => s.Tags.Select(t => new { s.StepId, t.TagId }))
            .ToArrayAsync();
        var tagSteps = stepTags
            .GroupBy(key => key.TagId)
            .ToDictionary(key => key.Key, values => values.Select(x => x.StepId).ToArray().AsEnumerable());
        return tagSteps;
    }
}
public interface ISubscriptionsProvider
{
    public Task<IEnumerable<JobSubscription>> GetJobSubscriptionsAsync();
    public Task<IEnumerable<JobTagSubscription>> GetJobTagSubscriptionsAsync();
    public Task<User?> GetLauncherUserAsync();
    public Task<IEnumerable<StepSubscription>> GetStepSubscriptionsAsync();
    public Task<IDictionary<Guid, IEnumerable<Guid>>> GetTagStepsAsync();
    public Task<IEnumerable<TagSubscription>> GetTagSubscriptionsAsync();
}