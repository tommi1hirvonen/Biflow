namespace Biflow.Ui;

public record DeleteJobTagCommand(Guid JobId, Guid TagId) : IRequest;

internal class DeleteJobTagCommandHandler(IDbContextFactory<AppDbContext> dbContextFactory) : IRequestHandler<DeleteJobTagCommand>
{
    public async Task Handle(DeleteJobTagCommand request, CancellationToken cancellationToken)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var tag = await context.JobTags
            .Include(t => t.Jobs.Where(s => s.JobId == request.JobId))
            .FirstOrDefaultAsync(t => t.TagId == request.TagId, cancellationToken);
        if (tag?.Jobs.FirstOrDefault(s => s.JobId == request.JobId) is { } job)
        {
            tag.Jobs.Remove(job);
            await context.SaveChangesAsync(cancellationToken);
        }
    }
}