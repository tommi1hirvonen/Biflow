namespace Biflow.Ui.Core;

public class CreateTabularStepCommand : CreateStepCommand<TabularStep>
{
    public required double TimeoutMinutes { get; init; }
    public required Guid ConnectionId { get; init; }
    public required string ModelName { get; init; }
    public required string? TableName { get; init; }
    public required string? PartitionName { get; init; }
}

[UsedImplicitly]
internal class CreateTabularStepCommandHandler(
    IDbContextFactory<AppDbContext> dbContextFactory, StepValidator validator)
    : CreateStepCommandHandler<CreateTabularStepCommand, TabularStep>(dbContextFactory, validator)
{
    protected override async Task<TabularStep> CreateStepAsync(CreateTabularStepCommand request, AppDbContext dbContext,
        CancellationToken cancellationToken)
    {
        // Check that the connection exists.
        if (!await dbContext.AnalysisServicesConnections
                .AnyAsync(c => c.ConnectionId == request.ConnectionId, cancellationToken))
        {
            throw new NotFoundException<AnalysisServicesConnection>(request.ConnectionId);
        }

        var step = new TabularStep
        {
            JobId = request.JobId,
            StepName = request.StepName,
            StepDescription = request.StepDescription,
            ExecutionPhase = request.ExecutionPhase,
            DuplicateExecutionBehaviour = request.DuplicateExecutionBehaviour,
            IsEnabled = request.IsEnabled,
            RetryAttempts = request.RetryAttempts,
            RetryIntervalMinutes = request.RetryIntervalMinutes,
            ExecutionConditionExpression = new EvaluationExpression
                { Expression = request.ExecutionConditionExpression },
            TimeoutMinutes = request.TimeoutMinutes,
            ConnectionId = request.ConnectionId,
            TabularModelName = request.ModelName,
            TabularTableName = request.TableName,
            TabularPartitionName = request.PartitionName
        };

        return step;
    }
}