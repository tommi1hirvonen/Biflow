namespace Biflow.Ui.Core;

public record DeleteStepTagCommand(Guid StepId, Guid TagId) : IRequest;

internal class DeleteStepTagCommandHandler(IDbContextFactory<AppDbContext> dbContextFactory) : IRequestHandler<DeleteStepTagCommand>
{
    public async Task Handle(DeleteStepTagCommand request, CancellationToken cancellationToken)
    {
        using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var tag = await context.Tags
            .Include(t => t.Steps.Where(s => s.StepId == request.StepId))
            .FirstOrDefaultAsync(t => t.TagId == request.TagId, cancellationToken);
        if (tag?.Steps.FirstOrDefault(s => s.StepId == request.StepId) is Step step)
        {
            tag.Steps.Remove(step);
            await context.SaveChangesAsync(cancellationToken);
        }
    }
}