namespace Biflow.Ui.Core;

public class CreateDatabricksStepCommand : CreateStepCommand<DatabricksStep>
{
    public required double TimeoutMinutes { get; init; }
    public required Guid DatabricksWorkspaceId { get; init; }
    public required DatabricksStepSettings DatabricksStepSettings { get; init; }
    public required CreateStepParameter[] Parameters { get; init; }
}

[UsedImplicitly]
internal class CreateDatabricksStepCommandHandler(
    IDbContextFactory<AppDbContext> dbContextFactory,
    StepValidator validator
) : CreateStepCommandHandler<CreateDatabricksStepCommand, DatabricksStep>(dbContextFactory, validator)
{
    protected override async Task<DatabricksStep> CreateStepAsync(
        CreateDatabricksStepCommand request, AppDbContext dbContext, CancellationToken cancellationToken)
    {
        // Check that the Databricks workspace exists.
        if (!await dbContext.DatabricksWorkspaces
                .AnyAsync(x => x.WorkspaceId == request.DatabricksWorkspaceId, cancellationToken))
        {
            throw new NotFoundException<DatabricksWorkspace>(request.DatabricksWorkspaceId);
        }
        
        var step = new DatabricksStep
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
            DatabricksWorkspaceId = request.DatabricksWorkspaceId,
            DatabricksStepSettings = request.DatabricksStepSettings
        };
        
        foreach (var createParameter in request.Parameters)
        {
            var parameter = new DatabricksStepParameter
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