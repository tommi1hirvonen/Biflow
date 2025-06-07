namespace Biflow.Ui.Core;

public record ToggleStepsEnabledCommand(Guid[] StepIds, bool IsEnabled) : IRequest;

[UsedImplicitly]
internal class ToggleStepsEnabledCommandHandler(IDbContextFactory<AppDbContext> dbContextFactory)
    : IRequestHandler<ToggleStepsEnabledCommand>
{
    public async Task Handle(ToggleStepsEnabledCommand request, CancellationToken cancellationToken)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        await context.Steps
            .Where(step => request.StepIds.Contains(step.StepId))
            .ExecuteUpdateAsync(x => x.SetProperty(step => step.IsEnabled, request.IsEnabled), cancellationToken);
    }
}