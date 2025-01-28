namespace Biflow.Ui.Core;

public class CreateQlikStepCommand : CreateStepCommand<QlikStep>
{
    public required double TimeoutMinutes { get; init; }
    public required Guid QlikCloudEnvironmentId { get; init; }
    public required QlikStepSettings Settings { get; init; }
}

[UsedImplicitly]
internal class CreateQlikStepCommandHandler(
    IDbContextFactory<AppDbContext> dbContextFactory,
    StepValidator validator
) : CreateStepCommandHandler<CreateQlikStepCommand, QlikStep>(dbContextFactory, validator)
{
    protected override async Task<QlikStep> CreateStepAsync(
        CreateQlikStepCommand request, AppDbContext dbContext, CancellationToken cancellationToken)
    {
        // Check that the Qlik Cloud environment exists.
        if (!await dbContext.QlikCloudEnvironments
                .AnyAsync(x => x.QlikCloudEnvironmentId == request.QlikCloudEnvironmentId, cancellationToken))
        {
            throw new NotFoundException<QlikCloudEnvironment>(request.QlikCloudEnvironmentId);
        }
        
        var step = new QlikStep
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
            QlikCloudEnvironmentId = request.QlikCloudEnvironmentId,
            QlikStepSettings = request.Settings
        };

        return step;
    }
}