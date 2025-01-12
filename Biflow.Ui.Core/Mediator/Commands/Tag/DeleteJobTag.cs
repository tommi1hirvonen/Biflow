namespace Biflow.Ui.Core;

public record DeleteJobTagCommand(Guid JobId, Guid TagId) : IRequest;

[UsedImplicitly]
internal class DeleteJobTagCommandHandler(IDbContextFactory<AppDbContext> dbContextFactory)
    : IRequestHandler<DeleteJobTagCommand>
{
    public async Task Handle(DeleteJobTagCommand request, CancellationToken cancellationToken)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var tag = await context.JobTags
            .Include(t => t.Jobs.Where(s => s.JobId == request.JobId))
            .FirstOrDefaultAsync(t => t.TagId == request.TagId, cancellationToken)
                  ?? throw new NotFoundException<JobTag>(request.TagId);
        var job = tag.Jobs.FirstOrDefault(s => s.JobId == request.JobId)
            ?? throw new NotFoundException<Job>(request.JobId);
        tag.Jobs.Remove(job);
        await context.SaveChangesAsync(cancellationToken);
    }
}