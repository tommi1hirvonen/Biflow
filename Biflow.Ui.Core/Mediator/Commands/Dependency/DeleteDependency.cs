namespace Biflow.Ui.Core;

public record DeleteDependencyCommand(Guid StepId, Guid DependentOnStepId) : IRequest;

internal class DeleteDependencyCommandHandler(IDbContextFactory<AppDbContext> dbContextFactory)
    : IRequestHandler<DeleteDependencyCommand>
{
    public async Task Handle(DeleteDependencyCommand request, CancellationToken cancellationToken)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        await context.Dependencies
            .Where(d => d.StepId == request.StepId && d.DependantOnStepId == request.DependentOnStepId)
            .ExecuteDeleteAsync(cancellationToken);

    }
}