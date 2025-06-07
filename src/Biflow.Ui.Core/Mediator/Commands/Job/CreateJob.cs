namespace Biflow.Ui.Core;

public record CreateJobCommand(
    string JobName,
    string? JobDescription,
    ExecutionMode ExecutionMode,
    bool StopOnFirstError,
    int MaxParallelSteps,
    double OvertimeNotificationLimitMinutes,
    double TimeoutMinutes,
    bool IsEnabled,
    bool IsPinned,
    Guid[] JobTagIds) : IRequest<Job>;

[UsedImplicitly]
internal class CreateJobCommandHandler(IDbContextFactory<AppDbContext> dbContextFactory)
    : IRequestHandler<CreateJobCommand, Job>
{
    public async Task<Job> Handle(CreateJobCommand request, CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        
        var jobTags = await dbContext.JobTags
            .Where(t => request.JobTagIds.Contains(t.TagId))
            .ToArrayAsync(cancellationToken);

        foreach (var id in request.JobTagIds)
        {
            if (jobTags.All(t => t.TagId != id))
            {
                throw new NotFoundException<JobTag>(id);
            }
        }
        
        var job = new Job
        {
            JobName = request.JobName,
            JobDescription = request.JobDescription,
            ExecutionMode = request.ExecutionMode,
            StopOnFirstError = request.StopOnFirstError,
            MaxParallelSteps = request.MaxParallelSteps,
            OvertimeNotificationLimitMinutes = request.OvertimeNotificationLimitMinutes,
            TimeoutMinutes = request.TimeoutMinutes,
            IsEnabled = request.IsEnabled,
            IsPinned = request.IsPinned
        };
        
        foreach (var tag in jobTags) job.Tags.Add(tag);
        
        job.EnsureDataAnnotationsValidated();
        
        dbContext.Jobs.Add(job);
        await dbContext.SaveChangesAsync(cancellationToken);
        
        return job;
    }
}