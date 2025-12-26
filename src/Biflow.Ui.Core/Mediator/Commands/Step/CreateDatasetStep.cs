namespace Biflow.Ui.Core;

public class CreateDatasetStepCommand : CreateStepCommand<DatasetStep>
{
    public required Guid FabricWorkspaceId { get; init; }
    public required Guid DatasetId { get; init; }
    public required string? DatasetName { get; init; }
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
        // Check that the Fabric workspace exists.
        if (!await dbContext.FabricWorkspaces
                .AnyAsync(x => x.FabricWorkspaceId == request.FabricWorkspaceId, cancellationToken))
        {
            throw new NotFoundException<FabricWorkspace>(request.FabricWorkspaceId);
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
            FabricWorkspaceId = request.FabricWorkspaceId,
            DatasetId = request.DatasetId.ToString(),
            DatasetName = request.DatasetName
        };

        return step;
    }
}