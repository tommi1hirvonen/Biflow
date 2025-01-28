namespace Biflow.Ui.Core;

public class CreateSqlStepCommand : CreateStepCommand<SqlStep>
{
    public required double TimeoutMinutes { get; init; }
    public required string SqlStatement { get; init; }
    public required Guid ConnectionId { get; init; }
    public required Guid? ResultCaptureJobParameterId { get; init; }
    public required CreateStepParameter[] Parameters { get; init; }
}

[UsedImplicitly]
internal class CreateSqlStepCommandHandler(
    IDbContextFactory<AppDbContext> dbContextFactory,
    StepValidator validator
    ) : CreateStepCommandHandler<CreateSqlStepCommand, SqlStep>(dbContextFactory, validator)
{
    protected override async Task<SqlStep> CreateStepAsync(
        CreateSqlStepCommand request, AppDbContext dbContext, CancellationToken cancellationToken)
    {
        // Check that the connection exists.
        if (!await dbContext.SqlConnections.AnyAsync(c => c.ConnectionId == request.ConnectionId, cancellationToken))
        {
            throw new NotFoundException<SqlConnectionBase>(request.ConnectionId);
        }
        
        // Check that the job parameter exists for this job.
        if (request.ResultCaptureJobParameterId is { } id &&
            !await dbContext.Set<JobParameter>()
                .AnyAsync(p => p.JobId == request.JobId && p.ParameterId == id, cancellationToken))
        {
            throw new NotFoundException<JobParameter>(("JobId", request.JobId), ("ParameterId", id));
        }
        
        var step = new SqlStep
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
            SqlStatement = request.SqlStatement,
            ConnectionId = request.ConnectionId,
            ResultCaptureJobParameterId = request.ResultCaptureJobParameterId
        };

        foreach (var createParameter in request.Parameters)
        {
            var parameter = new SqlStepParameter
            {
                ParameterName = createParameter.ParameterName,
                ParameterValue = createParameter.ParameterValue,
                UseExpression = createParameter.UseExpression,
                Expression = new EvaluationExpression { Expression = createParameter.Expression },
                InheritFromJobParameterId = createParameter.InheritFromJobParameterId
            };
            foreach (var createExpressionParameter in createParameter.ExpressionParameters)
            {
                parameter.AddExpressionParameter(
                    createExpressionParameter.ParameterName,
                    createExpressionParameter.InheritFromJobParameterId);
            }
            step.StepParameters.Add(parameter);
        }

        return step;
    }
}