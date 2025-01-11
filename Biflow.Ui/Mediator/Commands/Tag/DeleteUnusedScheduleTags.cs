namespace Biflow.Ui;

public record DeleteUnusedScheduleTagsCommand : IRequest<DeleteUnusedScheduleTagsResponse>;

public record DeleteUnusedScheduleTagsResponse(IEnumerable<ScheduleTag> DeletedTags);

internal class DeleteUnusedScheduleTagsCommandHandler(IDbContextFactory<AppDbContext> dbContextFactory)
    : IRequestHandler<DeleteUnusedScheduleTagsCommand, DeleteUnusedScheduleTagsResponse>
{
    public async Task<DeleteUnusedScheduleTagsResponse> Handle(DeleteUnusedScheduleTagsCommand request, CancellationToken cancellationToken)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var tags = await context.ScheduleTags
            .Where(t => !t.Schedules.Any())
            .ToArrayAsync(cancellationToken);
        context.ScheduleTags.RemoveRange(tags);
        await context.SaveChangesAsync(cancellationToken);
        return new(tags);
    }
}