namespace Biflow.Ui.Core;

public record UpdateDataObjectCommand(
    Guid ObjectId,
    string ObjectUri, 
    int MaxConcurrentWrites) : IRequest<DataObject>;

[UsedImplicitly]
internal class UpdateDataObjectCommandHandler(IDbContextFactory<AppDbContext> dbContextFactory)
    : IRequestHandler<UpdateDataObjectCommand, DataObject>
{
    public async Task<DataObject> Handle(UpdateDataObjectCommand request, CancellationToken cancellationToken)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var dataObject = await context.DataObjects
            .FirstOrDefaultAsync(x => x.ObjectId == request.ObjectId, cancellationToken)
            ?? throw new NotFoundException<DataObject>(request.ObjectId);
        
        dataObject.ObjectUri = request.ObjectUri;
        dataObject.MaxConcurrentWrites = request.MaxConcurrentWrites;
        
        dataObject.EnsureDataAnnotationsValidated();
        await context.SaveChangesAsync(cancellationToken);
        return dataObject;
    }
}