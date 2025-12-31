namespace Biflow.Ui.Core;

public record CreatePropertyTranslationCommand(
    Guid PropertyTranslationSetId,
    string PropertyTranslationName,
    int Order,
    string PropertyPath,
    string OldValue,
    bool ExactMatch,
    ParameterValue NewValue) : IRequest<PropertyTranslation>;

[UsedImplicitly]
internal class CreatePropertyTranslationCommandHandler(IDbContextFactory<AppDbContext> dbContextFactory)
    : IRequestHandler<CreatePropertyTranslationCommand, PropertyTranslation>
{
    public async Task<PropertyTranslation> Handle(CreatePropertyTranslationCommand request,
        CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        if (!await dbContext.PropertyTranslationSets
                .AnyAsync(x => x.PropertyTranslationSetId == request.PropertyTranslationSetId, cancellationToken))
        {
            throw new NotFoundException<PropertyTranslationSet>(request.PropertyTranslationSetId);
        }

        var propertyTranslation = new PropertyTranslation
        {
            PropertyTranslationSetId = request.PropertyTranslationSetId,
            PropertyTranslationName = request.PropertyTranslationName,
            Order = request.Order,
            PropertyPath = request.PropertyPath,
            NewValue = request.NewValue,
            // OldValue is only effective for string values.
            OldValue = request.NewValue.ValueType is ParameterValueType.String ? request.OldValue : "",
            // ExactMatch is only effective for string values.
            ExactMatch = request.NewValue.ValueType is ParameterValueType.String && request.ExactMatch
        };
        propertyTranslation.EnsureDataAnnotationsValidated();
        dbContext.PropertyTranslations.Add(propertyTranslation);
        await dbContext.SaveChangesAsync(cancellationToken);
        return propertyTranslation;
    }
}
