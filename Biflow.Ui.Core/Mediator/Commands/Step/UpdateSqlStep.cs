namespace Biflow.Ui.Core;

public class UpdateSqlStepCommand : UpdateStepCommand<SqlStep>
{
    public required int TimeoutMinutes { get; init; }
    public required string SqlStatement { get; init; }
    public required Guid ConnectionId { get; init; }
    public required Guid? ResultCaptureJobParameterId { get; init; }
}

[UsedImplicitly]
internal class UpdateSqlStepCommandHandler(
    IDbContextFactory<AppDbContext> dbContextFactory,
    StepValidator validator
) : UpdateStepCommandHandler<UpdateSqlStepCommand, SqlStep>(dbContextFactory, validator)
{
    protected override async Task UpdatePropertiesAsync(
        SqlStep step, UpdateSqlStepCommand request, AppDbContext dbContext, CancellationToken cancellationToken)
    {
        // Check that the connection exists.
        if (!await dbContext.SqlConnections.AnyAsync(c => c.ConnectionId == request.ConnectionId, cancellationToken))
        {
            throw new NotFoundException<SqlConnectionBase>(request.ConnectionId);
        }
        
        // Check that the job parameter exists for this job.
        if (request.ResultCaptureJobParameterId is { } id &&
            !await dbContext.Set<JobParameter>()
                .AnyAsync(p => p.JobId == step.JobId && p.ParameterId == id, cancellationToken))
        {
            throw new NotFoundException<JobParameter>(("JobId", step.JobId), ("ParameterId", id));
        }
        
        step.TimeoutMinutes = request.TimeoutMinutes;
        step.SqlStatement = request.SqlStatement;
        step.ConnectionId = request.ConnectionId;
        step.ResultCaptureJobParameterId = request.ResultCaptureJobParameterId;
    }
}