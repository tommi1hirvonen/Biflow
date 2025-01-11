namespace Biflow.Ui;

public record DeleteTagCommand(Guid TagId) : IRequest;

internal class DeleteTagCommandHandler(IDbContextFactory<AppDbContext> dbContextFactory) : IRequestHandler<DeleteTagCommand>
{
    public async Task Handle(DeleteTagCommand request, CancellationToken cancellationToken)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var tagToRemove = await context.Tags.FirstOrDefaultAsync(t => t.TagId == request.TagId, cancellationToken);
        if (tagToRemove is not null)
        {
            context.Tags.Remove(tagToRemove);
            await context.SaveChangesAsync(cancellationToken);
        }
    }
}