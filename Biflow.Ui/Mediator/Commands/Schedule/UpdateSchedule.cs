namespace Biflow.Ui;

public record UpdateScheduleCommand(Schedule Schedule) : IRequest;

internal class UpdateScheduleCommandHandler(
    IDbContextFactory<AppDbContext> dbContextFactory,
    ISchedulerService scheduler)
    : IRequestHandler<UpdateScheduleCommand>
{
    public async Task Handle(UpdateScheduleCommand request, CancellationToken cancellationToken)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        var stepTagIds = request.Schedule.TagFilter.Select(t => t.TagId).ToArray();
        var scheduleTagIds = request.Schedule.Tags.Select(t => t.TagId).ToArray();

        var stepTagsFromDb = await context.StepTags
            .Where(t => stepTagIds.Contains(t.TagId))
            .ToListAsync(cancellationToken);
        var scheduleTagsFromDb = await context.ScheduleTags
            .Where(t => scheduleTagIds.Contains(t.TagId))
            .ToListAsync(cancellationToken);
        var scheduleFromDb = await context.Schedules
            .Include(s => s.Tags)
            .Include(s => s.TagFilter)
            .FirstOrDefaultAsync(s => s.ScheduleId == request.Schedule.ScheduleId, cancellationToken);

        if (scheduleFromDb is null)
        {
            return;
        }

        scheduleFromDb.ScheduleName = request.Schedule.ScheduleName;
        scheduleFromDb.CronExpression = request.Schedule.CronExpression;
        scheduleFromDb.DisallowConcurrentExecution = request.Schedule.DisallowConcurrentExecution;

        // Synchronize step tags
        var stepTagsToAdd = request.Schedule.TagFilter
            .Where(t1 => scheduleFromDb.TagFilter.All(t2 => t2.TagId != t1.TagId))
            .Select(t => t.TagId);
        foreach (var id in stepTagsToAdd)
        {
            // New tags
            var tag = stepTagsFromDb.FirstOrDefault(t => t.TagId == id);
            if (tag is null)
            {
                continue;
            }
            scheduleFromDb.TagFilter.Add(tag);
        }
        var stepTagsToRemove = scheduleFromDb.TagFilter
            .Where(t => !stepTagIds.Contains(t.TagId))
            .ToArray(); // materialize since items may be removed from the sequence during iteration
        foreach (var tag in stepTagsToRemove)
        {
            scheduleFromDb.TagFilter.Remove(tag);
        }

        // Synchronize schedule tags
        var scheduleTagsToAdd = request.Schedule.Tags
            .Where(t1 => scheduleFromDb.Tags.All(t2 => t2.TagId != t1.TagId))
            .Select(t => (t.TagId, t.TagName, t.Color));
        foreach (var (id, name, color) in scheduleTagsToAdd)
        {
            var tag = scheduleTagsFromDb.FirstOrDefault(t => t.TagId == id)
                ?? new ScheduleTag(name) { Color = color };
            scheduleFromDb.Tags.Add(tag);
        }
        var scheduleTagsToRemove = scheduleFromDb.Tags
            .Where(t => !scheduleTagIds.Contains(t.TagId))
            .ToArray(); // materialize since items may be removed from the sequence during iteration
        foreach (var tag in scheduleTagsToRemove)
        {
            scheduleFromDb.Tags.Remove(tag);
        }

        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
        try
        {
            await scheduler.UpdateScheduleAsync(request.Schedule);
            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(CancellationToken.None);
            throw;
        }
    }
}