namespace Biflow.Ui.Core;

public record ToggleStepEnabledCommand(Guid StepId, bool IsEnabled) : IRequest;

internal class ToggleStepEnabledCommandHandler(IDbContextFactory<AppDbContext> dbContextFactory)
    : IRequestHandler<ToggleStepEnabledCommand>
{
    public async Task Handle(ToggleStepEnabledCommand request, CancellationToken cancellationToken)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var rowsUpdated = await context.Steps
            .Where(step => step.StepId == request.StepId)
            .ExecuteUpdateAsync(x => x.SetProperty(step => step.IsEnabled, request.IsEnabled), cancellationToken);
        if (rowsUpdated == 0)
        {
            throw new NotFoundException<Step>(request.StepId);
        }
    }
}