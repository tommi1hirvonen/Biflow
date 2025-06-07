using JetBrains.Annotations;

namespace Biflow.Ui;

public record UpdateStepExecutionAttemptStatusCommand(
    Guid ExecutionId, Guid StepId, int RetryAttemptIndex, StepExecutionStatus Status) : IRequest<StepExecutionAttempt>;

[UsedImplicitly]
internal class UpdateStepExecutionAttemptStatusCommandHandler(IDbContextFactory<AppDbContext> dbContextFactory)
    : IRequestHandler<UpdateStepExecutionAttemptStatusCommand, StepExecutionAttempt>
{
    public async Task<StepExecutionAttempt> Handle(UpdateStepExecutionAttemptStatusCommand request, CancellationToken cancellationToken)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var attempt = await context.StepExecutionAttempts
            .FirstOrDefaultAsync(e => e.ExecutionId == request.ExecutionId &&
                        e.StepId == request.StepId &&
                        e.RetryAttemptIndex == request.RetryAttemptIndex, cancellationToken)
            ?? throw new NotFoundException<StepExecutionAttempt>(
                (nameof(request.ExecutionId), request.ExecutionId),
                (nameof(request.StepId), request.StepId),
                (nameof(request.RetryAttemptIndex), request.RetryAttemptIndex));
        attempt.ExecutionStatus = request.Status;
        attempt.StartedOn ??= DateTimeOffset.Now;
        attempt.EndedOn ??= DateTimeOffset.Now;
        await context.SaveChangesAsync(cancellationToken);
        return attempt;
    }
}