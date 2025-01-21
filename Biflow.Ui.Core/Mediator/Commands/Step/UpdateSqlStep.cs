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
    protected override void UpdateProperties(SqlStep step, UpdateSqlStepCommand request)
    {
        step.TimeoutMinutes = request.TimeoutMinutes;
        step.SqlStatement = request.SqlStatement;
        step.ConnectionId = request.ConnectionId;
        step.ResultCaptureJobParameterId = request.ResultCaptureJobParameterId;
    }
}