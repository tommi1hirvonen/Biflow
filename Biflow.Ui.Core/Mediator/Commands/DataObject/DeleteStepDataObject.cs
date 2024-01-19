namespace Biflow.Ui.Core;

public record DeleteStepDataObjectCommand(StepDataObject Reference) : IRequest;

internal class DeleteStepDataObjectCommandHandler(IDbContextFactory<ServiceDbContext> dbContextFactory)
    : IRequestHandler<DeleteStepDataObjectCommand>
{
    public async Task Handle(DeleteStepDataObjectCommand request, CancellationToken cancellationToken)
    {
        using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        context.StepDataObjects.Remove(request.Reference);
        await context.SaveChangesAsync(cancellationToken);
    }
}