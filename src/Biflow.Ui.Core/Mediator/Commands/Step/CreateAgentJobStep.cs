namespace Biflow.Ui.Core;

public class CreateAgentJobStepCommand : CreateStepCommand<AgentJobStep>
{
    public required double TimeoutMinutes { get; init; }
    public required Guid ConnectionId { get; init; }
    public required string AgentJobName { get; init; }
}

[UsedImplicitly]
internal class CreateAgentJobStepCommandHandler(
    IDbContextFactory<AppDbContext> dbContextFactory, StepValidator validator)
    : CreateStepCommandHandler<CreateAgentJobStepCommand, AgentJobStep>(dbContextFactory, validator)
{
    protected override async Task<AgentJobStep> CreateStepAsync(
        CreateAgentJobStepCommand request,
        AppDbContext dbContext,
        CancellationToken cancellationToken)
    {
        // Check that the connection exists.
        if (!await dbContext.SqlConnections.AnyAsync(c => c.ConnectionId == request.ConnectionId, cancellationToken))
        {
            throw new NotFoundException<SqlConnectionBase>(request.ConnectionId);
        }

        var step = new AgentJobStep
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
            AgentJobName = request.AgentJobName
        };

        return step;
    }
}