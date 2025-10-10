namespace Biflow.Ui.Core;

public class CreateFunctionStepCommand : CreateStepCommand<FunctionStep>
{
    public required double TimeoutMinutes { get; init; }
    public required Guid? FunctionAppId { get; init; }
    public required string FunctionUrl { get; init; }
    public required string? FunctionInput { get; init; }
    public required FunctionInputFormat FunctionInputFormat { get; init; }
    public required bool DisableAsyncPattern { get; init; }
    public required string? FunctionKey { get; init; }
    public required CreateStepParameter[] Parameters { get; init; }
}

[UsedImplicitly]
internal class CreateFunctionStepCommandHandler(
    IDbContextFactory<AppDbContext> dbContextFactory,
    StepValidator validator
) : CreateStepCommandHandler<CreateFunctionStepCommand, FunctionStep>(dbContextFactory, validator)
{
    protected override async Task<FunctionStep> CreateStepAsync(
        CreateFunctionStepCommand request, AppDbContext dbContext, CancellationToken cancellationToken)
    {
        // Check that the function app exists.
        if (request.FunctionAppId is { } id &&
            !await dbContext.FunctionApps.AnyAsync(x => x.FunctionAppId == id, cancellationToken))
        {
            throw new NotFoundException<FunctionApp>(id);
        }
        
        var step = new FunctionStep
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
            FunctionAppId = request.FunctionAppId,
            FunctionUrl = request.FunctionUrl,
            FunctionInput = request.FunctionInput,
            FunctionInputFormat = request.FunctionInputFormat,
            DisableAsyncPattern = request.DisableAsyncPattern,
            FunctionKey = request.FunctionKey
        };
        
        foreach (var createParameter in request.Parameters)
        {
            var parameter = new FunctionStepParameter
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