namespace Biflow.Ui.Core;

public record UpdateScheduleCommand(
    Guid ScheduleId,
    string ScheduleName,
    string CronExpression,
    bool IsEnabled,
    bool DisallowConcurrentExecution,
    Guid[] ScheduleTagIds,
    Guid[] FilterStepTagIds) : IRequest<Schedule>;

[UsedImplicitly]
internal class UpdateScheduleCommandHandler(
    IDbContextFactory<AppDbContext> dbContextFactory,
    ISchedulerService scheduler) : IRequestHandler<UpdateScheduleCommand, Schedule>
{
    public async Task<Schedule> Handle(UpdateScheduleCommand request, CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        var scheduleTags = await dbContext.ScheduleTags
            .Where(t => request.ScheduleTagIds.Contains(t.TagId))
            .ToArrayAsync(cancellationToken);
        
        foreach (var id in request.ScheduleTagIds)
        {
            if (scheduleTags.All(t => t.TagId != id))
            {
                throw new NotFoundException<ScheduleTag>(id);
            }
        }
        
        var stepTags = await dbContext.StepTags
            .Where(t => request.FilterStepTagIds.Contains(t.TagId))
            .ToArrayAsync(cancellationToken);
        
        foreach (var id in request.FilterStepTagIds)
        {
            if (stepTags.All(t => t.TagId != id))
            {
                throw new NotFoundException<StepTag>(id);
            }
        }
        
        var schedule = await dbContext.Schedules
            .Include(s => s.Tags)
            .Include(s => s.TagFilter)
            .FirstOrDefaultAsync(s => s.ScheduleId == request.ScheduleId, cancellationToken)
            ?? throw new NotFoundException<Schedule>(request.ScheduleId);

        schedule.ScheduleName = request.ScheduleName;
        schedule.CronExpression = request.CronExpression;
        schedule.IsEnabled = request.IsEnabled;
        schedule.DisallowConcurrentExecution = request.DisallowConcurrentExecution;

        // Synchronize filter step tags
        var stepTagsToAdd = stepTags.Where(t1 => schedule.TagFilter.All(t2 => t2.TagId != t1.TagId));
        foreach (var tag in stepTagsToAdd)
        {
            schedule.TagFilter.Add(tag);
        }
        var stepTagsToRemove = schedule.TagFilter
            .Where(t => !request.FilterStepTagIds.Contains(t.TagId))
            .ToArray(); // materialize since items may be removed from the sequence during iteration
        foreach (var tag in stepTagsToRemove)
        {
            schedule.TagFilter.Remove(tag);
        }

        // Synchronize schedule tags
        var scheduleTagsToAdd = scheduleTags.Where(t1 => schedule.Tags.All(t2 => t2.TagId != t1.TagId));
        foreach (var tag in scheduleTagsToAdd)
        {
            schedule.Tags.Add(tag);
        }
        var scheduleTagsToRemove = schedule.Tags
            .Where(t => !request.ScheduleTagIds.Contains(t.TagId))
            .ToArray(); // materialize since items may be removed from the sequence during iteration
        foreach (var tag in scheduleTagsToRemove)
        {
            schedule.Tags.Remove(tag);
        }
        
        schedule.EnsureDataAnnotationsValidated();

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        try
        {
            await scheduler.UpdateScheduleAsync(schedule);
            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(CancellationToken.None);
            throw;
        }

        return schedule;
    }
}