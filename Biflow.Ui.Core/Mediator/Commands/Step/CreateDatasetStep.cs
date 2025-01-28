namespace Biflow.Ui.Core;

public class CreateDatasetStepCommand : CreateStepCommand<DatasetStep>
{
    public required Guid AzureCredentialId { get; init; }
    public required Guid WorkspaceId { get; init; }
    public required Guid DatasetId { get; init; }
}

[UsedImplicitly]
internal class CreateDatasetStepCommandHandler(
    IDbContextFactory<AppDbContext> dbContextFactory,
    StepValidator validator)
    : CreateStepCommandHandler<CreateDatasetStepCommand, DatasetStep>(dbContextFactory, validator)
{
    protected override async Task<DatasetStep> CreateStepAsync(CreateDatasetStepCommand request, AppDbContext dbContext,
        CancellationToken cancellationToken)
    {
        // Check that the Azure credential exists.
        if (!await dbContext.AzureCredentials
                .AnyAsync(x => x.AzureCredentialId == request.AzureCredentialId, cancellationToken))
        {
            throw new NotFoundException<AzureCredential>(request.AzureCredentialId);
        }

        var step = new DatasetStep
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
            AzureCredentialId = request.AzureCredentialId,
            WorkspaceId = request.WorkspaceId.ToString(),
            DatasetId = request.DatasetId.ToString(),
        };

        return step;
    }
}