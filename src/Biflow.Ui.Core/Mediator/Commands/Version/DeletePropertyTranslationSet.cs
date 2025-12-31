namespace Biflow.Ui.Core;

public record DeletePropertyTranslationSetCommand(Guid PropertyTranslationSetId) : IRequest;

[UsedImplicitly]
internal class DeletePropertyTranslationSetCommandHandler(IDbContextFactory<AppDbContext> dbContextFactory)
    : IRequestHandler<DeletePropertyTranslationSetCommand>
{
    public async Task Handle(DeletePropertyTranslationSetCommand request, CancellationToken cancellationToken)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var propertyTranslationSet = await context.PropertyTranslationSets
            .FirstOrDefaultAsync(x => x.PropertyTranslationSetId == request.PropertyTranslationSetId, cancellationToken)
            ?? throw new NotFoundException<PropertyTranslationSet>(request.PropertyTranslationSetId);
        context.PropertyTranslationSets.Remove(propertyTranslationSet);
        await context.SaveChangesAsync(cancellationToken);
    }
}
