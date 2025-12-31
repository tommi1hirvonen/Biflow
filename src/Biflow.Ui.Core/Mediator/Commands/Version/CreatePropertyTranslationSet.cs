namespace Biflow.Ui.Core;

public record CreatePropertyTranslationSetCommand(
    string PropertyTranslationSetName) : IRequest<PropertyTranslationSet>;

[UsedImplicitly]
internal class CreatePropertyTranslationSetCommandHandler(IDbContextFactory<AppDbContext> dbContextFactory)
    : IRequestHandler<CreatePropertyTranslationSetCommand, PropertyTranslationSet>
{
    public async Task<PropertyTranslationSet> Handle(CreatePropertyTranslationSetCommand request,
        CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var propertyTranslationSet = new PropertyTranslationSet
        {
            PropertyTranslationSetName = request.PropertyTranslationSetName
        };
        propertyTranslationSet.EnsureDataAnnotationsValidated();
        dbContext.PropertyTranslationSets.Add(propertyTranslationSet);
        await dbContext.SaveChangesAsync(cancellationToken);
        return propertyTranslationSet;
    }
}
