namespace Biflow.Ui.Core;

public class CreateDataflowStepCommand : CreateStepCommand<DataflowStep>
{
    public required double TimeoutMinutes { get; init; }
    public required Guid FabricWorkspaceId { get; init; }
    public required Guid DataflowId { get; init; }
    public required string DataflowName { get; init; }
}

[UsedImplicitly]
internal class CreateDataflowStepCommandHandler(
    IDbContextFactory<AppDbContext> dbContextFactory,
    StepValidator validator)
    : CreateStepCommandHandler<CreateDataflowStepCommand, DataflowStep>(dbContextFactory, validator)
{
    protected override async Task<DataflowStep> CreateStepAsync(CreateDataflowStepCommand request,
        AppDbContext dbContext, CancellationToken cancellationToken)
    {
        // Check that the Fabric workspace exists.
        if (!await dbContext.FabricWorkspaces
                .AnyAsync(x => x.FabricWorkspaceId == request.FabricWorkspaceId, cancellationToken))
        {
            throw new NotFoundException<FabricWorkspace>(request.FabricWorkspaceId);
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
            FabricWorkspaceId = request.FabricWorkspaceId,
            DataflowId = request.DataflowId.ToString(),
            DataflowName = request.DataflowName
        };

        return step;
    }
}