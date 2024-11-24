namespace Biflow.Ui.Core;

public record CreateScheduleCommand(Schedule Schedule) : IRequest;

internal class CreateScheduleCommandHandler(
    IDbContextFactory<AppDbContext> dbContextFactory,
    ISchedulerService scheduler)
    : IRequestHandler<CreateScheduleCommand>
{
    public async Task Handle(CreateScheduleCommand request, CancellationToken cancellationToken)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        // Synchronize step tags
        var stepTags = request.Schedule.TagFilter
            .Select(t => t.TagName)
            .Distinct()
            .ToArray();
        request.Schedule.TagFilter.Clear();
        var stepTagsFromDb = await context.StepTags
            .Where(t => stepTags.Contains(t.TagName))
            .ToListAsync(cancellationToken);
        foreach (var name in stepTags)
        {
            var tag = stepTagsFromDb.FirstOrDefault(t => t.TagName == name);
            if (tag is not null)
            {
                request.Schedule.TagFilter.Add(tag);
            }
        }

        // Synchronize schedule tags
        var scheduleTags = request.Schedule.Tags
            .Select(t => (t.TagName, t.Color))
            .Distinct()
            .ToArray();
        request.Schedule.Tags.Clear();
        var scheduleTagsFromDb = await context.ScheduleTags
            .Where(t1 => scheduleTags.Select(t2 => t2.TagName).Contains(t1.TagName))
            .ToListAsync(cancellationToken);
        foreach (var (name, color) in scheduleTags)
        {
            var tag = scheduleTagsFromDb.FirstOrDefault(t => t.TagName == name)
                ?? new(name) { Color = color };
            request.Schedule.Tags.Add(tag);
        }

        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            ArgumentNullException.ThrowIfNull(request.Schedule.TagFilter);
            context.Schedules.Add(request.Schedule);
            await context.SaveChangesAsync(cancellationToken);
            await scheduler.AddScheduleAsync(request.Schedule);
            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(CancellationToken.None);
            throw;
        }
    }
}