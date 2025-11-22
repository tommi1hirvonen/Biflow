using JetBrains.Annotations;

namespace Biflow.Ui.Mediator.Commands.DataObject;

public record DeleteStepDataObjectCommand(
    Guid StepId,
    Guid ObjectId,
    DataObjectReferenceType ReferenceType) : IRequest;

[UsedImplicitly]
internal class DeleteStepDataObjectCommandHandler(IDbContextFactory<AppDbContext> dbContextFactory)
    : IRequestHandler<DeleteStepDataObjectCommand>
{
    public async Task Handle(DeleteStepDataObjectCommand request, CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        
        var stepDataObject = await dbContext.StepDataObjects
            .FirstOrDefaultAsync(x => x.StepId == request.StepId &&
                                      x.ObjectId == request.ObjectId &&
                                      x.ReferenceType == request.ReferenceType, cancellationToken)
            ?? throw new NotFoundException<StepDataObject>(
                ("StepId", request.StepId),
                ("ObjectId", request.ObjectId),
                ("ReferenceType", request.ReferenceType));
        
        dbContext.StepDataObjects.Remove(stepDataObject);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}