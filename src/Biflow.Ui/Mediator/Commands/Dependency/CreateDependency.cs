using JetBrains.Annotations;

namespace Biflow.Ui;

internal record CreateDependencyCommand(Guid StepId, Guid DependentOnStepId, DependencyType DependencyType) : IRequest;

[UsedImplicitly]
internal class CreateDependencyCommandHandler(IDbContextFactory<AppDbContext> dbContextFactory)
    : IRequestHandler<CreateDependencyCommand>
{
    public async Task Handle(CreateDependencyCommand request, CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        var stepExists = await dbContext.Steps
            .AnyAsync(s => s.StepId == request.StepId, cancellationToken);
        if (!stepExists)
        {
            throw new NotFoundException<Step>(request.StepId);
        }

        var dependentOnStepExists = await dbContext.Steps
            .AnyAsync(s => s.StepId == request.DependentOnStepId, cancellationToken); 
        if (!dependentOnStepExists)
        {
            throw new NotFoundException<Step>(request.DependentOnStepId);
        }

        var dependency = new Dependency
        {
            StepId = request.StepId,
            DependantOnStepId = request.DependentOnStepId,
            DependencyType = request.DependencyType
        };
        dbContext.Dependencies.Add(dependency);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}