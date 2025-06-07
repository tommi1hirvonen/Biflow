namespace Biflow.Ui.Core;

public class CreateEmailStepCommand : CreateStepCommand<EmailStep>
{
    public required string[] Recipients { get; init; }
    public required string Subject { get; init; }
    public required string Body { get; init; }
    public required CreateStepParameter[] Parameters { get; init; }
}

[UsedImplicitly]
internal class CreateEmailStepCommandHandler(
    IDbContextFactory<AppDbContext> dbContextFactory,
    StepValidator validator
) : CreateStepCommandHandler<CreateEmailStepCommand, EmailStep>(dbContextFactory, validator)
{
    protected override Task<EmailStep> CreateStepAsync(
        CreateEmailStepCommand request, AppDbContext dbContext, CancellationToken cancellationToken)
    {
        var step = new EmailStep
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
            Recipients = string.Join(',', request.Recipients),
            Subject = request.Subject,
            Body = request.Body
        };
        
        foreach (var createParameter in request.Parameters)
        {
            var parameter = new EmailStepParameter
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