namespace Biflow.Ui.Core;

public record DeleteStepTagRelationCommand(Guid StepId, Guid TagId) : IRequest;

[UsedImplicitly]
internal class DeleteStepTagRelationCommandHandler(IDbContextFactory<AppDbContext> dbContextFactory)
    : IRequestHandler<DeleteStepTagRelationCommand>
{
    public async Task Handle(DeleteStepTagRelationCommand request, CancellationToken cancellationToken)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var tag = await context.StepTags
            .Include(t => t.Steps.Where(s => s.StepId == request.StepId))
            .FirstOrDefaultAsync(t => t.TagId == request.TagId, cancellationToken)
                ?? throw new NotFoundException<StepTag>(request.TagId);
        var step = tag.Steps.FirstOrDefault(s => s.StepId == request.StepId)
            ?? throw new NotFoundException<Step>(request.StepId);
        tag.Steps.Remove(step);
        await context.SaveChangesAsync(cancellationToken);
    }
}