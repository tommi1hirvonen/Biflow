namespace Biflow.Ui.Core;

public record UpdateStepDependenciesCommand(
    Guid StepId,
    IDictionary<Guid, DependencyType> Dependencies) : IRequest<Dependency[]>;

[UsedImplicitly]
internal class UpdateStepDependenciesCommandHandler(IDbContextFactory<AppDbContext> dbContextFactory)
    : IRequestHandler<UpdateStepDependenciesCommand, Dependency[]>
{
    public async Task<Dependency[]> Handle(UpdateStepDependenciesCommand request, CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var step = await dbContext.Steps
            .Include(s => s.Dependencies)
            .FirstOrDefaultAsync(s => s.StepId == request.StepId, cancellationToken)
            ?? throw new NotFoundException<Step>(request.StepId);
        
        // Fetch dependency step ids from DB and check matching ids.
        var dependentStepIds = request.Dependencies.Keys.ToArray();
        var dependentStepIdsFromDb = await dbContext.Steps
            .Where(s => dependentStepIds.Contains(s.StepId))
            .Select(s => s.StepId)
            .ToArrayAsync(cancellationToken);

        foreach (var id in dependentStepIds)
        {
            if (!dependentStepIdsFromDb.Contains(id))
            {
                throw new NotFoundException<Step>(id);
            }
        }

        foreach (var dependency in step.Dependencies)
        {
            if (request.Dependencies.TryGetValue(dependency.DependantOnStepId, out var dependencyType))
            {
                dependency.DependencyType = dependencyType;
            }
        }

        var dependenciesToAdd = request.Dependencies
            .Where(x => step.Dependencies.All(d => x.Key != d.DependantOnStepId))
            .Select(x => new Dependency
            {
                StepId = request.StepId,
                DependantOnStepId = x.Key,
                DependencyType = x.Value
            });
        
        foreach (var dependency in dependenciesToAdd) step.Dependencies.Add(dependency);
        
        var dependenciesToRemove = step.Dependencies
            .Where(d => request.Dependencies.All(x => x.Key != d.DependantOnStepId))
            .ToArray();
        
        foreach (var dependency in dependenciesToRemove) step.Dependencies.Remove(dependency);
        
        await dbContext.SaveChangesAsync(cancellationToken);
        
        return step.Dependencies.ToArray();
    }
}