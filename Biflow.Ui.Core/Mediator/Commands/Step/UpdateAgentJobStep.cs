namespace Biflow.Ui.Core;

public class UpdateAgentJobStepCommand : UpdateStepCommand<AgentJobStep>
{
    public required int TimeoutMinutes { get; init; }
    public required Guid ConnectionId { get; init; }
    public required string AgentJobName { get; init; }
}

[UsedImplicitly]
internal class UpdateAgentJobStepCommandHandler(
    IDbContextFactory<AppDbContext> dbContextFactory,
    StepValidator validator)
    : UpdateStepCommandHandler<UpdateAgentJobStepCommand, AgentJobStep>(dbContextFactory, validator)
{
    protected override Task<AgentJobStep?> GetStepAsync(Guid stepId, AppDbContext dbContext,
        CancellationToken cancellationToken)
    {
        return dbContext.AgentJobSteps
            .Include(step => step.Job)
            .ThenInclude(job => job.JobParameters)
            .Include(step => step.Tags)
            .Include(step => step.Dependencies)
            .Include(step => step.DataObjects)
            .ThenInclude(s => s.DataObject)
            .Include(step => step.ExecutionConditionParameters)
            .FirstOrDefaultAsync(step => step.StepId == stepId, cancellationToken);
    }

    protected override async Task UpdateTypeSpecificPropertiesAsync(AgentJobStep step, UpdateAgentJobStepCommand request, AppDbContext dbContext,
        CancellationToken cancellationToken)
    {
        // Check that the connection exists.
        if (!await dbContext.SqlConnections.AnyAsync(c => c.ConnectionId == request.ConnectionId, cancellationToken))
        {
            throw new NotFoundException<SqlConnectionBase>(request.ConnectionId);
        }
        
        step.TimeoutMinutes = request.TimeoutMinutes;
        step.ConnectionId = request.ConnectionId;
        step.AgentJobName = request.AgentJobName;
    }
}