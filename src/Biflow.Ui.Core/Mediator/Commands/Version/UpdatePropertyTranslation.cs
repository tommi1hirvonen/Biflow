namespace Biflow.Ui.Core;

public record UpdatePropertyTranslationCommand(
    Guid PropertyTranslationId,
    string PropertyTranslationName,
    int Order,
    string PropertyPath,
    string OldValue,
    bool ExactMatch,
    ParameterValue NewValue) : IRequest<PropertyTranslation>;

[UsedImplicitly]
internal class UpdatePropertyTranslationCommandHandler(IDbContextFactory<AppDbContext> dbContextFactory)
    : IRequestHandler<UpdatePropertyTranslationCommand, PropertyTranslation>
{
    public async Task<PropertyTranslation> Handle(UpdatePropertyTranslationCommand request,
        CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var propertyTranslation = await dbContext.PropertyTranslations
            .FirstOrDefaultAsync(x => x.PropertyTranslationId == request.PropertyTranslationId, cancellationToken)
            ?? throw new NotFoundException<PropertyTranslation>(request.PropertyTranslationId);
        propertyTranslation.PropertyTranslationName = request.PropertyTranslationName;
        propertyTranslation.Order = request.Order;
        propertyTranslation.PropertyPath = request.PropertyPath;
        propertyTranslation.NewValue = request.NewValue;
        // OldValue is only effective for string values.
        propertyTranslation.OldValue = request.NewValue.ValueType is ParameterValueType.String ? request.OldValue : "";
        // ExactMatch is only effective for string values.
        propertyTranslation.ExactMatch = request.NewValue.ValueType is ParameterValueType.String && request.ExactMatch;
        propertyTranslation.EnsureDataAnnotationsValidated();
        await dbContext.SaveChangesAsync(cancellationToken);
        return propertyTranslation;
    }
}
