using JetBrains.Annotations;

namespace Biflow.Ui.Mediator.Commands.StepExecutionAttempt;

public record UpdateStepExecutionAttemptStatusCommand(
    Guid ExecutionId, Guid StepId, int RetryAttemptIndex, StepExecutionStatus Status) : IRequest<Biflow.Core.Entities.StepExecutionAttempt>;

[UsedImplicitly]
internal class UpdateStepExecutionAttemptStatusCommandHandler(IDbContextFactory<AppDbContext> dbContextFactory)
    : IRequestHandler<UpdateStepExecutionAttemptStatusCommand, Biflow.Core.Entities.StepExecutionAttempt>
{
    public async Task<Biflow.Core.Entities.StepExecutionAttempt> Handle(UpdateStepExecutionAttemptStatusCommand request, CancellationToken cancellationToken)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var attempt = await context.StepExecutionAttempts
            .FirstOrDefaultAsync(e => e.ExecutionId == request.ExecutionId &&
                        e.StepId == request.StepId &&
                        e.RetryAttemptIndex == request.RetryAttemptIndex, cancellationToken)
            ?? throw new NotFoundException<Biflow.Core.Entities.StepExecutionAttempt>(
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