namespace Biflow.Ui.Core;

public class CreateFabricStepCommand : CreateStepCommand<FabricStep>
{
    public required double TimeoutMinutes { get; init; }
    public required FabricItemType ItemType { get; init; }
    public required Guid ItemId { get; init; }
    public required string ItemName { get; init; }
    public required Guid FabricWorkspaceId { get; init; }
    public required CreateStepParameter[] Parameters { get; init; }
}

[UsedImplicitly]
internal class CreateFabricStepCommandHandler(
    IDbContextFactory<AppDbContext> dbContextFactory,
    StepValidator validator
) : CreateStepCommandHandler<CreateFabricStepCommand, FabricStep>(dbContextFactory, validator)
{
    protected override async Task<FabricStep> CreateStepAsync(
        CreateFabricStepCommand request, AppDbContext dbContext, CancellationToken cancellationToken)
    {
        // Check that the Fabric workspace exists.
        if (!await dbContext.FabricWorkspaces
                .AnyAsync(x => x.FabricWorkspaceId == request.FabricWorkspaceId, cancellationToken))
        {
            throw new NotFoundException<FabricWorkspace>(request.FabricWorkspaceId);
        }
        
        var step = new FabricStep
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
            ItemType = request.ItemType,
            ItemId = request.ItemId,
            ItemName = request.ItemName,
            FabricWorkspaceId = request.FabricWorkspaceId
        };
        
        foreach (var createParameter in request.Parameters)
        {
            var parameter = new FabricStepParameter
            {
                ParameterName = createParameter.ParameterName,
                ParameterValue = createParameter.ParameterValue,
                UseExpression = createParameter.UseExpression,
                Expression = new EvaluationExpression { Expression = createParameter.Expression },
                InheritFromJobParameterId = createParameter.InheritFromJobParameterId
            };
            foreach (var createExpressionParameter in createParameter.ExpressionParameters)
            {
                parameter.AddExpressionParameter(
                    createExpressionParameter.ParameterName,
                    createExpressionParameter.InheritFromJobParameterId);
            }
            step.StepParameters.Add(parameter);
        }

        return step;
    }
}