namespace Biflow.Ui.Core;

public class UpdateEmailStepCommand : UpdateStepCommand<EmailStep>
{
    public required string[] Recipients { get; init; }
    public required string Subject { get; init; }
    public required string Body { get; init; }
    public required UpdateStepParameter[] Parameters { get; init; }
}

[UsedImplicitly]
internal class UpdateEmailStepCommandHandler(
    IDbContextFactory<AppDbContext> dbContextFactory,
    StepValidator validator
) : UpdateStepCommandHandler<UpdateEmailStepCommand, EmailStep>(dbContextFactory, validator)
{
    protected override Task<EmailStep?> GetStepAsync(
        Guid stepId, AppDbContext dbContext, CancellationToken cancellationToken)
    {
        return dbContext.EmailSteps
            .Include(step => step.StepParameters)
            .ThenInclude(p => p.InheritFromJobParameter)
            .Include(step => step.StepParameters)
            .ThenInclude(p => p.ExpressionParameters)
            .Include(step => step.Tags)
            .Include(step => step.Dependencies)
            .Include(step => step.DataObjects)
            .ThenInclude(s => s.DataObject)
            .Include(step => step.ExecutionConditionParameters)
            .FirstOrDefaultAsync(step => step.StepId == stepId, cancellationToken);
    }
    
    protected override async Task UpdateTypeSpecificPropertiesAsync(
        EmailStep step, UpdateEmailStepCommand request, AppDbContext dbContext, CancellationToken cancellationToken)
    {
        step.Recipients = string.Join(',', step.Recipients);
        step.Subject = request.Subject;
        step.Body = request.Body;
        
        await SynchronizeParametersAsync<EmailStepParameter, UpdateStepParameter>(
            step,
            request.Parameters,
            parameter => new EmailStepParameter
            {
                ParameterName = parameter.ParameterName,
                ParameterValue = parameter.ParameterValue,
                UseExpression = parameter.UseExpression,
                Expression = new EvaluationExpression { Expression = parameter.Expression },
                InheritFromJobParameterId = parameter.InheritFromJobParameterId
            },
            dbContext,
            cancellationToken);
    }
}