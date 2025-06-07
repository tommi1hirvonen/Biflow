namespace Biflow.Ui.Core;

public record DeleteJobTagRelationCommand(Guid JobId, Guid TagId) : IRequest;

[UsedImplicitly]
internal class DeleteJobTagRelationCommandHandler(IDbContextFactory<AppDbContext> dbContextFactory)
    : IRequestHandler<DeleteJobTagRelationCommand>
{
    public async Task Handle(DeleteJobTagRelationCommand request, CancellationToken cancellationToken)
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