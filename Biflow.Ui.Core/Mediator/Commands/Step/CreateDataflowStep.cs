namespace Biflow.Ui.Core;

public class CreateDataflowStepCommand : CreateStepCommand<DataflowStep>
{
    public required double TimeoutMinutes { get; init; }
    public required Guid AzureCredentialId { get; init; }
    public required Guid WorkspaceId { get; init; }
    public required Guid DataflowId { get; init; }
}

[UsedImplicitly]
internal class CreateDataflowStepCommandHandler(
    IDbContextFactory<AppDbContext> dbContextFactory,
    StepValidator validator)
    : CreateStepCommandHandler<CreateDataflowStepCommand, DataflowStep>(dbContextFactory, validator)
{
    protected override async Task<DataflowStep> CreateStepAsync(CreateDataflowStepCommand request, AppDbContext dbContext,
        CancellationToken cancellationToken)
    {
        // Check that the Azure credential exists.
        if (!await dbContext.AzureCredentials
                .AnyAsync(x => x.AzureCredentialId == request.AzureCredentialId, cancellationToken))
        {
            throw new NotFoundException<AzureCredential>(request.AzureCredentialId);
        }

        var step = new DataflowStep
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
            AzureCredentialId = request.AzureCredentialId,
            WorkspaceId = request.WorkspaceId.ToString(),
            DataflowId = request.DataflowId.ToString(),
        };

        return step;
    }
}