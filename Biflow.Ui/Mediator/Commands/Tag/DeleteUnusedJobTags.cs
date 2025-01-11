namespace Biflow.Ui;

public record DeleteUnusedJobTagsCommand : IRequest<DeleteUnusedJobTagsResponse>;

public record DeleteUnusedJobTagsResponse(IEnumerable<JobTag> DeletedTags);

internal class DeleteUnusedJobTagsCommandHandler(IDbContextFactory<AppDbContext> dbContextFactory)
    : IRequestHandler<DeleteUnusedJobTagsCommand, DeleteUnusedJobTagsResponse>
{
    public async Task<DeleteUnusedJobTagsResponse> Handle(DeleteUnusedJobTagsCommand request, CancellationToken cancellationToken)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var tags = await context.JobTags
            .Where(t => !t.Jobs.Any())
            .ToArrayAsync(cancellationToken);
        context.JobTags.RemoveRange(tags);
        await context.SaveChangesAsync(cancellationToken);
        return new(tags);
    }
}