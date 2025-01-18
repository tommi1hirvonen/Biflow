namespace Biflow.Ui.Api.Mediator.Commands;

internal record CreateDataObjectCommand(string ObjectUri, int MaxConcurrentWrites) : IRequest<DataObject>;

[UsedImplicitly]
internal class CreateDataObjectCommandHandler(IDbContextFactory<AppDbContext> dbContextFactory)
    : IRequestHandler<CreateDataObjectCommand, DataObject>
{
    public async Task<DataObject> Handle(CreateDataObjectCommand request, CancellationToken cancellationToken)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var dataObject = new DataObject
        {
            ObjectUri = request.ObjectUri,
            MaxConcurrentWrites = request.MaxConcurrentWrites
        };
        dataObject.EnsureDataAnnotationsValidated();
        context.DataObjects.Add(dataObject);
        await context.SaveChangesAsync(cancellationToken);
        return dataObject;
    }
}