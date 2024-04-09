namespace Biflow.Ui.Core;

public record UpdateScheduleCommand(Schedule Schedule, ICollection<string> Tags) : IRequest;

internal class UpdateScheduleCommandHandler(
    IDbContextFactory<AppDbContext> dbContextFactory,
    ISchedulerService scheduler)
    : IRequestHandler<UpdateScheduleCommand>
{
    public async Task Handle(UpdateScheduleCommand request, CancellationToken cancellationToken)
    {
        using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        context.Attach(request.Schedule).State = EntityState.Modified;
        var tags = await context.StepTags
            .Where(t => request.Tags.Contains(t.TagName))
            .ToListAsync(cancellationToken);

        // Synchronize tags
        foreach (var name in request.Tags.Where(str => !request.Schedule.TagFilter.Any(t => t.TagName == str)))
        {
            // New tags
            var tag = tags.FirstOrDefault(t => t.TagName == name) ?? new StepTag(name);
            request.Schedule.TagFilter.Add(tag);
        }
        foreach (var tag in request.Schedule.TagFilter.Where(t => !request.Tags.Contains(t.TagName)).ToArray())
        {
            request.Schedule.TagFilter.Remove(tag);
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