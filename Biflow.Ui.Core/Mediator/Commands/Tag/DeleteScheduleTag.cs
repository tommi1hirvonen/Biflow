namespace Biflow.Ui.Core;

public record DeleteScheduleTagCommand(Guid TagId) : IRequest;

[UsedImplicitly]
internal class DeleteScheduleTagCommandHandler(IDbContextFactory<AppDbContext> dbContextFactory)
    : IRequestHandler<DeleteScheduleTagCommand>
{
    public async Task Handle(DeleteScheduleTagCommand request, CancellationToken cancellationToken)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var tagToRemove = await context.ScheduleTags
            .FirstOrDefaultAsync(t => t.TagId == request.TagId, cancellationToken)
                ?? throw new NotFoundException<ScheduleTag>(request.TagId);
        context.ScheduleTags.Remove(tagToRemove);
        await context.SaveChangesAsync(cancellationToken);
    }
}