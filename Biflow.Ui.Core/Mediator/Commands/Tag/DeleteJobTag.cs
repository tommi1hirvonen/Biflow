namespace Biflow.Ui.Core;

public record DeleteJobTagCommand(Guid TagId) : IRequest;

[UsedImplicitly]
internal class DeleteJobTagCommandHandler(IDbContextFactory<AppDbContext> dbContextFactory)
    : IRequestHandler<DeleteJobTagCommand>
{
    public async Task Handle(DeleteJobTagCommand request, CancellationToken cancellationToken)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var tagToRemove = await context.JobTags
            .FirstOrDefaultAsync(t => t.TagId == request.TagId, cancellationToken)
                ?? throw new NotFoundException<JobTag>(request.TagId);
        context.JobTags.Remove(tagToRemove);
        await context.SaveChangesAsync(cancellationToken);
    }
}