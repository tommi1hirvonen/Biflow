namespace Biflow.Ui.Core;

public record DeleteStepsCommand(params Guid[] StepIds) : IRequest
{
    public DeleteStepsCommand(IEnumerable<Step> steps) : this(steps.Select(s => s.StepId).ToArray()) { }
}

internal class DeleteStepsCommandHandler(IDbContextFactory<AppDbContext> dbContextFactory) : IRequestHandler<DeleteStepsCommand>
{
    public async Task Handle(DeleteStepsCommand request, CancellationToken cancellationToken)
    {
        using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var steps = await context.Steps
            .Include(s => s.Dependencies)
            .Include(s => s.Depending)
            .Include($"{nameof(IHasStepParameters.StepParameters)}")
            .Include(s => s.StepSubscriptions)
            .Where(s => request.StepIds.Contains(s.StepId))
            .ToArrayAsync(cancellationToken);
        context.Steps.RemoveRange(steps);
        await context.SaveChangesAsync(cancellationToken);
    }
}