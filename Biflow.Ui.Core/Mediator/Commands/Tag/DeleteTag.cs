namespace Biflow.Ui.Core;

public record DeleteTagCommand(Guid TagId) : IRequest;

internal class DeleteTagCommandHandler(IDbContextFactory<AppDbContext> dbContextFactory) : IRequestHandler<DeleteTagCommand>
{
    public async Task Handle(DeleteTagCommand request, CancellationToken cancellationToken)
    {
        using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var tagToRemove = await context.StepTags.FirstOrDefaultAsync(t => t.TagId == request.TagId, cancellationToken);
        if (tagToRemove is not null)
        {
            context.StepTags.Remove(tagToRemove);
            await context.SaveChangesAsync(cancellationToken);
        }
    }
}