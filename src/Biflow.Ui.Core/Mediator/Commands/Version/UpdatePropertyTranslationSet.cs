namespace Biflow.Ui.Core;

public record UpdatePropertyTranslationSetCommand(
    Guid PropertyTranslationSetId,
    string PropertyTranslationSetName) : IRequest<PropertyTranslationSet>;

[UsedImplicitly]
internal class UpdatePropertyTranslationSetCommandHandler(IDbContextFactory<AppDbContext> dbContextFactory)
    : IRequestHandler<UpdatePropertyTranslationSetCommand, PropertyTranslationSet>
{
    public async Task<PropertyTranslationSet> Handle(UpdatePropertyTranslationSetCommand request,
        CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var propertyTranslationSet = await dbContext.PropertyTranslationSets
            .FirstOrDefaultAsync(x => x.PropertyTranslationSetId == request.PropertyTranslationSetId, cancellationToken)
            ?? throw new NotFoundException<PropertyTranslationSet>(request.PropertyTranslationSetId);
        propertyTranslationSet.PropertyTranslationSetName = request.PropertyTranslationSetName;
        propertyTranslationSet.EnsureDataAnnotationsValidated();
        await dbContext.SaveChangesAsync(cancellationToken);
        return propertyTranslationSet;
    }
}
