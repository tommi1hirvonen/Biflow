namespace Biflow.Ui.Core;

public record DeleteStepTagCommand(Guid TagId) : IRequest;

[UsedImplicitly]
internal class DeleteStepTagCommandHandler(IDbContextFactory<AppDbContext> dbContextFactory)
    : IRequestHandler<DeleteStepTagCommand>
{
    public async Task Handle(DeleteStepTagCommand request, CancellationToken cancellationToken)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var tagToRemove = await context.StepTags
            .FirstOrDefaultAsync(t => t.TagId == request.TagId, cancellationToken)
                ?? throw new NotFoundException<StepTag>(request.TagId);
        context.StepTags.Remove(tagToRemove);
        await context.SaveChangesAsync(cancellationToken);
    }
}