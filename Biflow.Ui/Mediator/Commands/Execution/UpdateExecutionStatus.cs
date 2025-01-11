namespace Biflow.Ui;

public record UpdateExecutionStatusCommand(Guid[] ExecutionIds, ExecutionStatus Status) : IRequest;

internal class UpdateExecutionStatusCommandHandler(IDbContextFactory<AppDbContext> dbContextFactory)
    : IRequestHandler<UpdateExecutionStatusCommand>
{
    public async Task Handle(UpdateExecutionStatusCommand request, CancellationToken cancellationToken)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var executions = await context.Executions
            .Where(e => request.ExecutionIds.Contains(e.ExecutionId))
            .ToArrayAsync(cancellationToken);
        foreach (var execution in executions)
        {
            execution.ExecutionStatus = request.Status;
            execution.StartedOn ??= DateTimeOffset.Now;
            execution.EndedOn ??= DateTimeOffset.Now;
        }
        await context.SaveChangesAsync(cancellationToken);
    }
}