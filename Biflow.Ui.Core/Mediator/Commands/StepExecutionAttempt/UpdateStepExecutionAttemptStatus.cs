namespace Biflow.Ui.Core;

public record UpdateStepExecutionAttemptStatusCommand(
    Guid ExecutionId, Guid StepId, int RetryAttemptIndex, StepExecutionStatus Status) : IRequest<StepExecutionAttempt>;

internal class UpdateStepExecutionAttemptStatusCommandHandler(IDbContextFactory<AppDbContext> dbContextFactory)
    : IRequestHandler<UpdateStepExecutionAttemptStatusCommand, StepExecutionAttempt>
{
    public async Task<StepExecutionAttempt> Handle(UpdateStepExecutionAttemptStatusCommand request, CancellationToken cancellationToken)
    {
        using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var attempt = await context.StepExecutionAttempts
            .Where(e => e.ExecutionId == request.ExecutionId && e.StepId == request.StepId && e.RetryAttemptIndex == request.RetryAttemptIndex)
            .FirstAsync(cancellationToken);
        attempt.ExecutionStatus = request.Status;
        attempt.StartedOn ??= DateTimeOffset.Now;
        attempt.EndedOn ??= DateTimeOffset.Now;
        await context.SaveChangesAsync(cancellationToken);
        return attempt;
    }
}