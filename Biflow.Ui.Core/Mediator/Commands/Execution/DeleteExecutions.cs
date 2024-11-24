namespace Biflow.Ui.Core;

public record DeleteExecutionsCommand(DateTimeOffset RangeStart, DateTimeOffset RangeEnd) : IRequest;

internal class DeleteExecutionsCommandHandler(IDbContextFactory<ServiceDbContext> dbContextFactory)
    : IRequestHandler<DeleteExecutionsCommand>
{
    public async Task Handle(DeleteExecutionsCommand request, CancellationToken cancellationToken)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        await using var transaction = context.Database.BeginTransaction();
        try
        {
            // Specify delete commands with joins for tables that do not have cascade deleted enabled.

            // Step parameter expression parameters
            await (
                from parameter in context.StepExecutionParameterExpressionParameters
                join execution in context.Executions on parameter.ExecutionId equals execution.ExecutionId into executions
                from execution in executions
                where execution.CreatedOn >= request.RangeStart && execution.CreatedOn <= request.RangeEnd
                select parameter
                ).ExecuteDeleteAsync(cancellationToken);

            // Monitoring steps
            await (
                from monitor in context.StepExecutionMonitors
                join execution in context.Executions on monitor.MonitoredExecutionId equals execution.ExecutionId into executions
                from execution in executions
                where execution.CreatedOn >= request.RangeStart && execution.CreatedOn <= request.RangeEnd
                select monitor
                ).ExecuteDeleteAsync(cancellationToken);

            // Monitored steps
            await (
                from monitor in context.StepExecutionMonitors
                join execution in context.Executions on monitor.ExecutionId equals execution.ExecutionId into executions
                from execution in executions
                where execution.CreatedOn >= request.RangeStart && execution.CreatedOn <= request.RangeEnd
                select monitor
                ).ExecuteDeleteAsync(cancellationToken);

            // Step executions
            await (
                from step in context.StepExecutions
                join execution in context.Executions on step.ExecutionId equals execution.ExecutionId into executions
                from execution in executions
                where execution.CreatedOn >= request.RangeStart && execution.CreatedOn <= request.RangeEnd
                select step
                ).ExecuteDeleteAsync(cancellationToken);

            await context.Executions
                .Where(e => e.CreatedOn >= request.RangeStart && e.CreatedOn <= request.RangeEnd)
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