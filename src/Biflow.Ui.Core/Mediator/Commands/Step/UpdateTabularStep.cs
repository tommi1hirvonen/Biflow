namespace Biflow.Ui.Core;

public class UpdateTabularStepCommand : UpdateStepCommand<TabularStep>
{
    public required double TimeoutMinutes { get; init; }
    public required Guid ConnectionId { get; init; }
    public required string ModelName { get; init; }
    public required string? TableName { get; init; }
    public required string? PartitionName { get; init; }
}

[UsedImplicitly]
internal class UpdateTabularStepCommandHandler(
    IDbContextFactory<AppDbContext> dbContextFactory,
    StepValidator validator)
    : UpdateStepCommandHandler<UpdateTabularStepCommand, TabularStep>(dbContextFactory, validator)
{
    protected override Task<TabularStep?> GetStepAsync(Guid stepId, AppDbContext dbContext,
        CancellationToken cancellationToken)
    {
        return dbContext.TabularSteps
            .Include(step => step.Tags)
            .Include(step => step.Dependencies)
            .Include(step => step.DataObjects)
            .ThenInclude(s => s.DataObject)
            .Include(step => step.ExecutionConditionParameters)
            .FirstOrDefaultAsync(step => step.StepId == stepId, cancellationToken);
    }

    protected override async Task UpdateTypeSpecificPropertiesAsync(TabularStep step, UpdateTabularStepCommand request,
        AppDbContext dbContext, CancellationToken cancellationToken)
    {
        // Check that the connection exists.
        if (!await dbContext.AnalysisServicesConnections
                .AnyAsync(c => c.ConnectionId == request.ConnectionId, cancellationToken))
        {
            throw new NotFoundException<AnalysisServicesConnection>(request.ConnectionId);
        }
        
        step.TimeoutMinutes = request.TimeoutMinutes;
        step.ConnectionId = request.ConnectionId;
        step.TabularModelName = request.ModelName;
        step.TabularTableName = request.TableName;
        step.TabularPartitionName = request.PartitionName;
    }
}