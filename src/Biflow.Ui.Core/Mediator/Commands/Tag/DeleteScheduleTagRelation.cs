namespace Biflow.Ui.Core;

public record DeleteScheduleTagRelationCommand(Guid ScheduleId, Guid TagId) : IRequest;

[UsedImplicitly]
internal class DeleteScheduleTagRelationCommandHandler(IDbContextFactory<AppDbContext> dbContextFactory)
    : IRequestHandler<DeleteScheduleTagRelationCommand>
{
    public async Task Handle(DeleteScheduleTagRelationCommand request, CancellationToken cancellationToken)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var tag = await context.ScheduleTags
            .Include(t => t.Schedules.Where(s => s.ScheduleId == request.ScheduleId))
            .FirstOrDefaultAsync(t => t.TagId == request.TagId, cancellationToken)
                ?? throw new NotFoundException<ScheduleTag>(request.TagId);
        var schedule = tag.Schedules.FirstOrDefault(s => s.ScheduleId == request.ScheduleId)
            ?? throw new NotFoundException<Schedule>(request.ScheduleId);
        tag.Schedules.Remove(schedule);
        await context.SaveChangesAsync(cancellationToken);
    }
}