using Biflow.Core.Interfaces;

namespace Biflow.Ui.Api.Mediator.Queries.Executions;

internal record StepExecutionQuery(
    Guid ExecutionId,
    Guid StepId,
    bool IncludeAttempts = false,
    bool IncludeDependencies = false,
    bool IncludeMonitors = false,
    bool IncludeDataObjects = false,
    bool IncludeParameters = false) : IRequest<StepExecution>;

[UsedImplicitly]
internal class StepExecutionQueryHandler(IDbContextFactory<ServiceDbContext> dbContextFactory)
    : IRequestHandler<StepExecutionQuery, StepExecution>
{
    public async Task<StepExecution> Handle(StepExecutionQuery request, CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var query = dbContext.StepExecutions
            .AsNoTrackingWithIdentityResolution();
        if (request.IncludeAttempts)
        {
            query = query.Include(e => e.StepExecutionAttempts);
        }
        if (request.IncludeDependencies)
        {
            query = query.Include(e => e.ExecutionDependencies);
        }
        if (request.IncludeMonitors)
        {
            query = query
                .Include(e => e.MonitoringStepExecutions)
                .Include(e => e.MonitoredStepExecutions);
        }
        if (request.IncludeDataObjects)
        {
            query = query.Include(e => e.DataObjects);
        }
        if (request.IncludeParameters)
        {
            query = query
                .Include(
                    $"{nameof(IHasStepExecutionParameters.StepExecutionParameters)}.{nameof(StepExecutionParameterBase.ExpressionParameters)}")
                .Include(e => e.ExecutionConditionParameters);
        }
        var stepExecution = await query
            .FirstOrDefaultAsync(e => e.ExecutionId == request.ExecutionId && e.StepId == request.StepId,
                cancellationToken);
        return stepExecution ?? throw new NotFoundException<StepExecution>(
            (nameof(StepExecution.ExecutionId), request.ExecutionId),
            (nameof(StepExecution.StepId), request.StepId));
    }
}