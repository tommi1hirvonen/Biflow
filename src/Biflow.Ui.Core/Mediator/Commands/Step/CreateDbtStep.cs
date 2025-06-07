namespace Biflow.Ui.Core;

public class CreateDbtStepCommand : CreateStepCommand<DbtStep>
{
    public required double TimeoutMinutes { get; init; }
    public required Guid DbtAccountId { get; init; }
    public required DbtJobDetails DbtJob { get; init; }
}

[UsedImplicitly]
internal class CreateDbtStepCommandHandler(
    IDbContextFactory<AppDbContext> dbContextFactory,
    StepValidator validator
) : CreateStepCommandHandler<CreateDbtStepCommand, DbtStep>(dbContextFactory, validator)
{
    protected override async Task<DbtStep> CreateStepAsync(
        CreateDbtStepCommand request, AppDbContext dbContext, CancellationToken cancellationToken)
    {
        // Check that the Dbt workspace exists.
        if (!await dbContext.DbtAccounts
                .AnyAsync(x => x.DbtAccountId == request.DbtAccountId, cancellationToken))
        {
            throw new NotFoundException<DbtAccount>(request.DbtAccountId);
        }
        
        var step = new DbtStep
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
            DbtAccountId = request.DbtAccountId,
            DbtJob = request.DbtJob
        };

        return step;
    }
}