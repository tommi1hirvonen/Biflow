namespace Biflow.Ui.Api.Mediator.Commands;

internal record CreateStepTagRelationCommand(Guid StepId, Guid TagId) : IRequest;

[UsedImplicitly]
internal class CreateStepTagRelationCommandHandler(IDbContextFactory<ServiceDbContext> dbContextFactory)
    : IRequestHandler<CreateStepTagRelationCommand>
{
    public async Task Handle(CreateStepTagRelationCommand request, CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var step = await dbContext.Steps
            .Include(j => j.Tags)
            .FirstOrDefaultAsync(j => j.StepId == request.StepId, cancellationToken)
                ?? throw new NotFoundException<Step>(request.StepId);
        var tag = await dbContext.StepTags
            .FirstOrDefaultAsync(t => t.TagId == request.TagId, cancellationToken)
                  ?? throw new NotFoundException<StepTag>(request.TagId);
        step.Tags.Add(tag);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}