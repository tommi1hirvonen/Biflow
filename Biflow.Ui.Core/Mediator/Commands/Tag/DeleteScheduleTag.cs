namespace Biflow.Ui.Core;

public record DeleteScheduleTagCommand(Guid ScheduleId, Guid TagId) : IRequest;

internal class DeleteScheduleTagCommandHandler(IDbContextFactory<AppDbContext> dbContextFactory) : IRequestHandler<DeleteScheduleTagCommand>
{
    public async Task Handle(DeleteScheduleTagCommand request, CancellationToken cancellationToken)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var tag = await context.ScheduleTags
            .Include(t => t.Schedules.Where(s => s.ScheduleId == request.ScheduleId))
            .FirstOrDefaultAsync(t => t.TagId == request.TagId, cancellationToken);
        if (tag?.Schedules.FirstOrDefault(s => s.ScheduleId == request.ScheduleId) is { } job)
        {
            tag.Schedules.Remove(job);
            await context.SaveChangesAsync(cancellationToken);
        }
    }
}