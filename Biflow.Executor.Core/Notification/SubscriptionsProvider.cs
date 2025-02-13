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

    public async Task<IEnumerable<StepTagSubscription>> GetTagSubscriptionsAsync()
    {
        var subsQuery = _dbContext.StepTagSubscriptions
            .AsNoTracking()
            .Include(s => s.User);
        var steps =
            from exec in _dbContext.StepExecutions
            join step in _dbContext.Steps on exec.StepId equals step.StepId into es
            from step in es.DefaultIfEmpty()
            where exec.ExecutionId == _execution.ExecutionId
            select step;
        var subscriptions = await subsQuery
            .Where(sub => sub.User.Email != null && steps.Any(step => step.Tags.Any(tag => tag.TagId == sub.TagId)))
            .ToArrayAsync();
        return subscriptions;
    }

    public async Task<IEnumerable<JobStepTagSubscription>> GetJobTagSubscriptionsAsync()
    {
        var subsQuery = _dbContext.JobStepTagSubscriptions
            .AsNoTracking()
            .Include(s => s.User);
        var steps =
            from exec in _dbContext.StepExecutions
            join step in _dbContext.Steps on exec.StepId equals step.StepId into es
            from step in es.DefaultIfEmpty()
            where exec.ExecutionId == _execution.ExecutionId
            select step;
        var subscriptions = await subsQuery
            .Where(sub => sub.User.Email != null && sub.JobId == _execution.JobId && steps.Any(step => step.Tags.Any(tag => tag.TagId == sub.TagId)))
            .ToArrayAsync();
        return subscriptions;
    }

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