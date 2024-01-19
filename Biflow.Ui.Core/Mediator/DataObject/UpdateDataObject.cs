namespace Biflow.Ui.Core;

public record UpdateDataObjectCommand(DataObject DataObject) : IRequest;

internal class UpdateDataObjectCommandHandler(IDbContextFactory<ServiceDbContext> dbContextFactory)
    : IRequestHandler<UpdateDataObjectCommand>
{
    public async Task Handle(UpdateDataObjectCommand request, CancellationToken cancellationToken)
    {
        using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        context.Attach(request.DataObject).State = EntityState.Modified;
        await context.SaveChangesAsync(cancellationToken);
    }
}