namespace Biflow.Ui.Core;

public record DeleteExecutionCommand(Guid ExecutionId) : IRequest;

internal class DeleteExecutionCommandHandler(IDbContextFactory<ServiceDbContext> dbContextFactory)
    : IRequestHandler<DeleteExecutionCommand>
{
    public async Task Handle(DeleteExecutionCommand request, CancellationToken cancellationToken)
    {
        using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var execution = await context.Executions
                .Include($"{nameof(Execution.StepExecutions)}.{nameof(IHasStepExecutionParameters.StepExecutionParameters)}.{nameof(StepExecutionParameterBase.ExpressionParameters)}")
                .Include(e => e.StepExecutions).ThenInclude(e => e.MonitoredStepExecutions)
                .Include(e => e.StepExecutions).ThenInclude(e => e.MonitoringStepExecutions)
                .FirstOrDefaultAsync(e => e.ExecutionId == request.ExecutionId, cancellationToken);
        if (execution is not null)
        {
            context.Executions.Remove(execution);
            await context.SaveChangesAsync(cancellationToken);
        }
    }
}