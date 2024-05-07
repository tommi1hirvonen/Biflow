namespace Biflow.Ui.Core;

public record DeleteExecutionCommand(Guid ExecutionId) : IRequest;

internal class DeleteExecutionCommandHandler(IDbContextFactory<ServiceDbContext> dbContextFactory)
    : IRequestHandler<DeleteExecutionCommand>
{
    public async Task Handle(DeleteExecutionCommand request, CancellationToken cancellationToken)
    {
        using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        using var transaction = context.Database.BeginTransaction();
        try
        {
            // Specify delete commands with joins for tables that do not have cascade deleted enabled.

            // Step parameter expression parameters
            await context.StepExecutionParameterExpressionParameters
                .Where(e => e.ExecutionId == request.ExecutionId)
                .ExecuteDeleteAsync(cancellationToken);

            // Monitoring steps
            await context.StepExecutionMonitors
                .Where(e => e.ExecutionId == request.ExecutionId)
                .ExecuteDeleteAsync(cancellationToken);

            // Monitored steps
            await context.StepExecutionMonitors
                .Where(e => e.MonitoredExecutionId == request.ExecutionId)
                .ExecuteDeleteAsync(cancellationToken);

            // Step executions
            await context.StepExecutions
                .Where(e => e.ExecutionId == request.ExecutionId)
                .ExecuteDeleteAsync(cancellationToken);

            await context.Executions
                .Where(e => e.ExecutionId == request.ExecutionId)
                .ExecuteDeleteAsync(cancellationToken);

            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }
}