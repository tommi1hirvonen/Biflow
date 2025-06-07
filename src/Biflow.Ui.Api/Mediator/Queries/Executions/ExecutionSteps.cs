using Biflow.Core.Interfaces;

namespace Biflow.Ui.Api.Mediator.Queries.Executions;

internal record ExecutionStepsQuery(
    Guid ExecutionId,
    bool IncludeAttempts,
    bool IncludeDependencies,
    bool IncludeMonitors,
    bool IncludeDataObjects,
    bool IncludeParameters) : IRequest<StepExecution[]>;

[UsedImplicitly]
internal class ExecutionStepsQueryHandler(IDbContextFactory<ServiceDbContext> dbContextFactory)
    : IRequestHandler<ExecutionStepsQuery, StepExecution[]>
{
    public async Task<StepExecution[]> Handle(ExecutionStepsQuery request, CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var executionExists = await dbContext.Executions
            .AnyAsync(e => e.ExecutionId == request.ExecutionId, cancellationToken);
        if (!executionExists)
        {
            throw new NotFoundException<Execution>(request.ExecutionId);
        }
        var query = dbContext.StepExecutions
            .AsNoTrackingWithIdentityResolution()
            .Where(e => e.ExecutionId == request.ExecutionId);
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
        var stepExecutions = await query.ToArrayAsync(cancellationToken);
        return stepExecutions;
    }
}