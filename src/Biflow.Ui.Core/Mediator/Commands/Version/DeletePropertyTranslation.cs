namespace Biflow.Ui.Core;

public record DeletePropertyTranslationCommand(Guid PropertyTranslationId) : IRequest;

[UsedImplicitly]
internal class DeletePropertyTranslationCommandHandler(IDbContextFactory<AppDbContext> dbContextFactory)
    : IRequestHandler<DeletePropertyTranslationCommand>
{
    public async Task Handle(DeletePropertyTranslationCommand request, CancellationToken cancellationToken)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var propertyTranslation = await context.PropertyTranslations
            .FirstOrDefaultAsync(x => x.PropertyTranslationId == request.PropertyTranslationId, cancellationToken)
            ?? throw new NotFoundException<PropertyTranslation>(request.PropertyTranslationId);
        context.PropertyTranslations.Remove(propertyTranslation);
        await context.SaveChangesAsync(cancellationToken);
    }
}
