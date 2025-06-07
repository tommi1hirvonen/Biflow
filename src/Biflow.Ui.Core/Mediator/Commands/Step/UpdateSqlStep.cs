namespace Biflow.Ui.Core;

public class UpdateSqlStepCommand : UpdateStepCommand<SqlStep>
{
    public required double TimeoutMinutes { get; init; }
    public required string SqlStatement { get; init; }
    public required Guid ConnectionId { get; init; }
    public required Guid? ResultCaptureJobParameterId { get; init; }
    public required UpdateStepParameter[] Parameters { get; init; }
}

[UsedImplicitly]
internal class UpdateSqlStepCommandHandler(
    IDbContextFactory<AppDbContext> dbContextFactory,
    StepValidator validator
) : UpdateStepCommandHandler<UpdateSqlStepCommand, SqlStep>(dbContextFactory, validator)
{
    protected override Task<SqlStep?> GetStepAsync(
        Guid stepId, AppDbContext dbContext, CancellationToken cancellationToken)
    {
        return dbContext.SqlSteps
            .Include(step => step.StepParameters)
            .ThenInclude(p => p.InheritFromJobParameter)
            .Include(step => step.StepParameters)
            .ThenInclude(p => p.ExpressionParameters)
            .Include(step => step.Tags)
            .Include(step => step.Dependencies)
            .Include(step => step.DataObjects)
            .ThenInclude(s => s.DataObject)
            .Include(step => step.ExecutionConditionParameters)
            .FirstOrDefaultAsync(step => step.StepId == stepId, cancellationToken);
    }
    
    protected override async Task UpdateTypeSpecificPropertiesAsync(SqlStep step, UpdateSqlStepCommand request,
        AppDbContext dbContext, CancellationToken cancellationToken)
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
        
        await SynchronizeParametersAsync<SqlStepParameter, UpdateStepParameter>(
            step,
            request.Parameters,
            parameter => new SqlStepParameter
            {
                ParameterName = parameter.ParameterName,
                ParameterValue = parameter.ParameterValue,
                UseExpression = parameter.UseExpression,
                Expression = new EvaluationExpression { Expression = parameter.Expression },
                InheritFromJobParameterId = parameter.InheritFromJobParameterId
            },
            dbContext,
            cancellationToken);
    }
}