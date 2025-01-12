namespace Biflow.Ui.Core;

public record DeleteStepsCommand(params Guid[] StepIds) : IRequest<Step[]>
{
    public DeleteStepsCommand(IEnumerable<Step> steps) : this(steps.Select(s => s.StepId).ToArray())
    {
    }
}

[UsedImplicitly]
internal class DeleteStepsCommandHandler(IDbContextFactory<AppDbContext> dbContextFactory)
    : IRequestHandler<DeleteStepsCommand, Step[]>
{
    public async Task<Step[]> Handle(DeleteStepsCommand request, CancellationToken cancellationToken)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var steps = await context.Steps
            .Include(s => s.Dependencies)
            .Include(s => s.Depending)
            .Include($"{nameof(IHasStepParameters.StepParameters)}")
            .Where(s => request.StepIds.Contains(s.StepId))
            .ToArrayAsync(cancellationToken);
        context.Steps.RemoveRange(steps);
        await context.SaveChangesAsync(cancellationToken);
        return steps;
    }
}