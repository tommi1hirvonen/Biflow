namespace Biflow.Ui.Core;

public class CreatePipelineStepCommand : CreateStepCommand<PipelineStep>
{
    public required double TimeoutMinutes { get; init; }
    public required Guid PipelineClientId { get; init; }
    public required string PipelineName { get; init; }
    public required CreateStepParameter[] Parameters { get; init; }
}

[UsedImplicitly]
internal class CreatePipelineStepCommandHandler(
    IDbContextFactory<AppDbContext> dbContextFactory,
    StepValidator validator
) : CreateStepCommandHandler<CreatePipelineStepCommand, PipelineStep>(dbContextFactory, validator)
{
    protected override async Task<PipelineStep> CreateStepAsync(
        CreatePipelineStepCommand request, AppDbContext dbContext, CancellationToken cancellationToken)
    {
        // Check that the pipeline client exists.
        if (!await dbContext.PipelineClients.AnyAsync(x => x.PipelineClientId == request.PipelineClientId, cancellationToken))
        {
            throw new NotFoundException<PipelineClient>(request.PipelineClientId);
        }
        
        var step = new PipelineStep
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
            PipelineClientId = request.PipelineClientId,
            PipelineName = request.PipelineName
        };
        
        foreach (var createParameter in request.Parameters)
        {
            var parameter = new PipelineStepParameter
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