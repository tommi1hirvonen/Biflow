namespace Biflow.Ui.Core;

public class CreateExeStepCommand : CreateStepCommand<ExeStep>
{
    public required double TimeoutMinutes { get; init; }
    public required string FilePath { get; init; }
    public required string? Arguments { get; init; }
    public required string? WorkingDirectory { get; init; }
    public required int? SuccessExitCode { get; init; }
    public required Guid? RunAsCredentialId { get; init; }
    public required Guid? ProxyId { get; init; }
    public required CreateStepParameter[] Parameters { get; init; }
}

[UsedImplicitly]
internal class CreateExeStepCommandHandler(
    IDbContextFactory<AppDbContext> dbContextFactory,
    StepValidator validator
) : CreateStepCommandHandler<CreateExeStepCommand, ExeStep>(dbContextFactory, validator)
{
    protected override async Task<ExeStep> CreateStepAsync(
        CreateExeStepCommand request, AppDbContext dbContext, CancellationToken cancellationToken)
    {
        // Check that the credential exists.
        if (request.RunAsCredentialId is { } id &&
            !await dbContext.Credentials.AnyAsync(x => x.CredentialId == id, cancellationToken))
        {
            throw new NotFoundException<Credential>(id);
        }
        
        var step = new ExeStep
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
            ExeFileName = request.FilePath,
            ExeArguments = request.Arguments,
            ExeWorkingDirectory = request.WorkingDirectory,
            ExeSuccessExitCode = request.SuccessExitCode,
            RunAsCredentialId = request.RunAsCredentialId,
            ProxyId = request.ProxyId
        };
        
        foreach (var createParameter in request.Parameters)
        {
            var parameter = new ExeStepParameter
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