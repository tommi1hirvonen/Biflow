namespace Biflow.Ui.Core;

public class CreateWaitStepCommand : CreateStepCommand<WaitStep>
{
    public required int WaitSeconds { get; init; }
}

[UsedImplicitly]
internal class CreateWaitStepCommandHandler(
    IDbContextFactory<AppDbContext> dbContextFactory,
    StepValidator validator)
    : CreateStepCommandHandler<CreateWaitStepCommand, WaitStep>(dbContextFactory, validator)
{
    protected override Task<WaitStep> CreateStepAsync(
        CreateWaitStepCommand request, AppDbContext dbContext, CancellationToken cancellationToken)
    {
        return Task.FromResult(new WaitStep
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
            WaitSeconds = request.WaitSeconds
        });
    }
}