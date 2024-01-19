namespace Biflow.Ui.Core;

public record CreateDependencyCommand(Dependency Dependency) : IRequest;

internal class CreateDependencyCommandHandler(IDbContextFactory<ServiceDbContext> dbContextFactory) : IRequestHandler<CreateDependencyCommand>
{
    public async Task Handle(CreateDependencyCommand request, CancellationToken cancellationToken)
    {
        using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var exists = await context.Dependencies
            .AnyAsync(d => d.StepId == request.Dependency.StepId && d.DependantOnStepId == request.Dependency.DependantOnStepId,
                cancellationToken);
        if (!exists)
        {
            request.Dependency.Step = null!;
            request.Dependency.DependantOnStep = null!;
            context.Dependencies.Add(request.Dependency);
            await context.SaveChangesAsync(cancellationToken);
        }
    }
}