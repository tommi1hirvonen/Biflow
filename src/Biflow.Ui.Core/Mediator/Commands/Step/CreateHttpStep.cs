namespace Biflow.Ui.Core;

public class CreateHttpStepCommand : CreateStepCommand<HttpStep>
{
    public required double TimeoutMinutes { get; init; }
    public required string Url { get; init; }
    public required HttpStepMethod Method { get; init; }
    public required string? Body { get; init; }
    public required HttpBodyFormat BodyFormat { get; init; }
    public required HttpHeader[] Headers { get; init; }
    public required bool DisableAsyncPattern { get; init; }
    public required CreateStepParameter[] Parameters { get; init; }
}

[UsedImplicitly]
internal class CreateHttpStepCommandHandler(
    IDbContextFactory<AppDbContext> dbContextFactory,
    StepValidator validator)
    : CreateStepCommandHandler<CreateHttpStepCommand, HttpStep>(dbContextFactory, validator)
{
    protected override Task<HttpStep> CreateStepAsync(
        CreateHttpStepCommand request, AppDbContext dbContext, CancellationToken cancellationToken)
    {
        var step = new HttpStep
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
            Url = request.Url,
            Method = request.Method,
            Body = request.Body,
            BodyFormat = request.BodyFormat,
            Headers = request.Headers.ToList(),
            DisableAsyncPattern = request.DisableAsyncPattern
        };
            
        foreach (var createParameter in request.Parameters)
        {
            var parameter = new HttpStepParameter
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

        return Task.FromResult(step);
    }
}