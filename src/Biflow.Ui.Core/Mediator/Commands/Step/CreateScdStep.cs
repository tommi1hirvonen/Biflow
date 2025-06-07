namespace Biflow.Ui.Core;

public class CreateScdStepCommand : CreateStepCommand<ScdStep>
{
    public required double TimeoutMinutes { get; init; }
    public required Guid ScdTableId { get; init; }
}

[UsedImplicitly]
internal class CreateScdStepCommandHandler(
    IDbContextFactory<AppDbContext> dbContextFactory,
    StepValidator validator
) : CreateStepCommandHandler<CreateScdStepCommand, ScdStep>(dbContextFactory, validator)
{
    protected override async Task<ScdStep> CreateStepAsync(
        CreateScdStepCommand request, AppDbContext dbContext, CancellationToken cancellationToken)
    {
        // Check that the SCD table exists.
        if (!await dbContext.ScdTables.AnyAsync(x => x.ScdTableId == request.ScdTableId, cancellationToken))
        {
            throw new NotFoundException<ScdTable>(request.ScdTableId);
        }
        
        var step = new ScdStep
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
            ScdTableId = request.ScdTableId
        };

        return step;
    }
}