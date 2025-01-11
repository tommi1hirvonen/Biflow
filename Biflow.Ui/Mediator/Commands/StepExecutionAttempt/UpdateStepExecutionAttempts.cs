namespace Biflow.Ui;

public record UpdateStepExecutionAttemptsCommand(IEnumerable<StepExecutionAttempt> Attempts) : IRequest;

internal class UpdateStepExecutionAttemptsCommandHandler(IDbContextFactory<AppDbContext> dbContextFactory)
    : IRequestHandler<UpdateStepExecutionAttemptsCommand>
{
    public async Task Handle(UpdateStepExecutionAttemptsCommand request, CancellationToken cancellationToken)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        foreach (var attempt in request.Attempts)
        {
            context.Attach(attempt).State = EntityState.Modified;
        }
        await context.SaveChangesAsync(cancellationToken);
    }
}