namespace Biflow.Ui.Core;

[UsedImplicitly]
public record UpdateJobCommand(
    Guid JobId,
    string JobName,
    string? JobDescription,
    ExecutionMode ExecutionMode,
    bool StopOnFirstError,
    double OvertimeNotificationLimitMinutes,
    double TimeoutMinutes,
    Guid[] JobTagIds) : IRequest<Job>;

[UsedImplicitly]
internal class UpdateJobCommandHandler(IDbContextFactory<AppDbContext> dbContextFactory)
    : IRequestHandler<UpdateJobCommand, Job>
{
    public async Task<Job> Handle(UpdateJobCommand request, CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        
        var job = await dbContext.Jobs
            .Include(j => j.Tags)
            .FirstOrDefaultAsync(j => j.JobId == request.JobId, cancellationToken)
            ?? throw new NotFoundException<Job>(request.JobId);
        
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
        
        job.JobName = request.JobName;
        job.JobDescription = request.JobDescription;
        job.ExecutionMode = request.ExecutionMode;
        job.StopOnFirstError = request.StopOnFirstError;
        job.OvertimeNotificationLimitMinutes = request.OvertimeNotificationLimitMinutes;
        job.TimeoutMinutes = request.TimeoutMinutes;
        
        // Synchronize tags
        var jobTagsToAdd = jobTags.Where(t1 => job.Tags.All(t2 => t2.TagId != t1.TagId));
        foreach (var tag in jobTagsToAdd)
        {
            job.Tags.Add(tag);
        }
        var jobTagsToRemove = job.Tags
            .Where(t => !request.JobTagIds.Contains(t.TagId))
            .ToArray(); // Materialize results because items may be removed from the sequence during iteration.
        foreach (var tag in jobTagsToRemove)
        {
            job.Tags.Remove(tag);
        }
        
        job.EnsureDataAnnotationsValidated();
        
        await dbContext.SaveChangesAsync(cancellationToken);
        return job;
    }
}