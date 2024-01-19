namespace Biflow.Ui.Core;

public record ToggleStepsCommand(Guid[] StepIds, bool IsEnabled) : IRequest
{
    public ToggleStepsCommand(Guid stepId, bool isEnabled)
        : this([stepId], isEnabled) { }

    public ToggleStepsCommand(IEnumerable<Step> steps,  bool isEnabled)
        : this(steps.Select(s => s.StepId).Distinct().ToArray(), isEnabled) { }
}

internal class ToggleStepsCommandHandler(IDbContextFactory<ServiceDbContext> dbContextFactory)
    : IRequestHandler<ToggleStepsCommand>
{
    public async Task Handle(ToggleStepsCommand request, CancellationToken cancellationToken)
    {
        using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        await context.Steps
            .Where(step => request.StepIds.Contains(step.StepId))
            .ExecuteUpdateAsync(x => x.SetProperty(step => step.IsEnabled, request.IsEnabled), cancellationToken);
    }
}