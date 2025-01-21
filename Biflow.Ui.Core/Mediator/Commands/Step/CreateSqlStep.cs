namespace Biflow.Ui.Core;

public class CreateSqlStepCommand : CreateStepCommand<SqlStep>
{
    public required int TimeoutMinutes { get; init; }
    public required string SqlStatement { get; init; }
    public required Guid ConnectionId { get; init; }
    public required Guid? ResultCaptureJobParameterId { get; init; }
}

[UsedImplicitly]
internal class CreateSqlStepCommandHandler(
    IDbContextFactory<AppDbContext> dbContextFactory,
    StepValidator validator
    ) : CreateStepCommandHandler<CreateSqlStepCommand, SqlStep>(dbContextFactory, validator)
{
    protected override Guid GetJobId(CreateSqlStepCommand request) => request.JobId;

    protected override Guid[] GetStepTagIds(CreateSqlStepCommand request) => request.StepTagIds;

    protected override SqlStep CreateStep(CreateSqlStepCommand request) => new()
    {
        JobId = request.JobId,
        StepName = request.StepName,
        StepDescription = request.StepDescription,
        ExecutionPhase = request.ExecutionPhase,
        DuplicateExecutionBehaviour = request.DuplicateExecutionBehaviour,
        IsEnabled = request.IsEnabled,
        RetryAttempts = request.RetryAttempts,
        RetryIntervalMinutes = request.RetryIntervalMinutes,
        ExecutionConditionExpression = new EvaluationExpression { Expression = request.ExecutionConditionExpression },
        TimeoutMinutes = request.TimeoutMinutes,
        SqlStatement = request.SqlStatement,
        ConnectionId = request.ConnectionId,
        ResultCaptureJobParameterId = request.ResultCaptureJobParameterId
    };
}