using JetBrains.Annotations;

namespace Biflow.Ui.Mediator.Commands.Dependency;

internal record DeleteDependencyCommand(Guid StepId, Guid DependentOnStepId) : IRequest;

[UsedImplicitly]
internal class DeleteDependencyCommandHandler(IDbContextFactory<AppDbContext> dbContextFactory)
    : IRequestHandler<DeleteDependencyCommand>
{
    public async Task Handle(DeleteDependencyCommand request, CancellationToken cancellationToken)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var deletedRows = await context.Dependencies
            .Where(d => d.StepId == request.StepId && d.DependantOnStepId == request.DependentOnStepId)
            .ExecuteDeleteAsync(cancellationToken);
        if (deletedRows == 0)
        {
            throw new NotFoundException<Biflow.Core.Entities.Dependency>(
                ("StepId", request.StepId),
                ("DependentOnStepId", request.DependentOnStepId));
        }

    }
}