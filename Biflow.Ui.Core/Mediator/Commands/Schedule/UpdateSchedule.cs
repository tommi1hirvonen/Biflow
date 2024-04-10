namespace Biflow.Ui.Core;

public record UpdateScheduleCommand(
    Schedule Schedule,
    ICollection<string> TagFilter,
    ICollection<string> Tags) : IRequest;

internal class UpdateScheduleCommandHandler(
    IDbContextFactory<AppDbContext> dbContextFactory,
    ISchedulerService scheduler)
    : IRequestHandler<UpdateScheduleCommand>
{
    public async Task Handle(UpdateScheduleCommand request, CancellationToken cancellationToken)
    {
        using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        context.Attach(request.Schedule).State = EntityState.Modified;

        // Synchronize step tags
        var stepTags = await context.StepTags
            .Where(t => request.TagFilter.Contains(t.TagName))
            .ToListAsync(cancellationToken);
        foreach (var name in request.TagFilter.Where(str => !request.Schedule.TagFilter.Any(t => t.TagName == str)))
        {
            // New tags
            var tag = stepTags.FirstOrDefault(t => t.TagName == name) ?? new StepTag(name);
            request.Schedule.TagFilter.Add(tag);
        }
        foreach (var tag in request.Schedule.TagFilter.Where(t => !request.TagFilter.Contains(t.TagName)).ToArray())
        {
            request.Schedule.TagFilter.Remove(tag);
        }

        // Synchronize schedule tags
        var scheduleTags = await context.ScheduleTags
            .Where(t => request.Tags.Contains(t.TagName))
            .ToListAsync(cancellationToken);
        foreach (var name in request.Tags.Where(str => !request.Schedule.Tags.Any(t => t.TagName == str)))
        {
            var tag = scheduleTags.FirstOrDefault(t => t.TagName == name) ?? new ScheduleTag(name);
            request.Schedule.Tags.Add(tag);
        }
        foreach (var tag in request.Schedule.Tags.Where(t => !request.Tags.Contains(t.TagName)).ToArray())
        {
            request.Schedule.Tags.Remove(tag);
        }

        using var transaction = context.Database.BeginTransaction();
        await context.SaveChangesAsync(cancellationToken);
        try
        {
            await scheduler.UpdateScheduleAsync(request.Schedule);
            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }
}