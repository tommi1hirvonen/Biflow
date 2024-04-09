namespace Biflow.Ui.Core;

public record CreateScheduleCommand(
    Schedule Schedule,
    ICollection<string> TagFilter,
    ICollection<string> Tags) : IRequest;

internal class CreateScheduleCommandHandler(
    IDbContextFactory<AppDbContext> dbContextFactory,
    ISchedulerService scheduler)
    : IRequestHandler<CreateScheduleCommand>
{
    public async Task Handle(CreateScheduleCommand request, CancellationToken cancellationToken)
    {
        using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        // Synchronize step tags
        request.Schedule.TagFilter.Clear();
        var stepTags = await context.StepTags
            .Where(t => request.TagFilter.Contains(t.TagName))
            .ToListAsync(cancellationToken);
        foreach (var tagName in request.TagFilter)
        {
            var tag = stepTags.FirstOrDefault(t => t.TagName == tagName);
            if (tag is not null)
            {
                request.Schedule.TagFilter.Add(tag);
            }
        }

        // Synchronize schedule tags
        request.Schedule.Tags.Clear();
        var scheduleTags = await context.ScheduleTags
            .Where(t => request.Tags.Contains(t.TagName))
            .ToListAsync(cancellationToken);
        foreach (var tagName in request.Tags)
        {
            var tag = scheduleTags.FirstOrDefault(t => t.TagName == tagName) ?? new(tagName);
            request.Schedule.Tags.Add(tag);
        }

        using var transaction = context.Database.BeginTransaction();
        try
        {
            ArgumentNullException.ThrowIfNull(request.Schedule.TagFilter);
            context.Schedules.Add(request.Schedule);
            await context.SaveChangesAsync(cancellationToken);
            await scheduler.AddScheduleAsync(request.Schedule);
            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }
}