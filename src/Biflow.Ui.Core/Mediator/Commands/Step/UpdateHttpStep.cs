namespace Biflow.Ui.Core;

public class UpdateHttpStepCommand : UpdateStepCommand<HttpStep>
{
    public required double TimeoutMinutes { get; init; }
    public required string Url { get; init; }
    public required HttpStepMethod Method { get; init; }
    public required string? Body { get; init; }
    public required HttpBodyFormat BodyFormat { get; init; }
    public required HttpHeader[] Headers { get; init; }
    public required bool DisableAsyncPattern { get; init; }
    public required UpdateStepParameter[] Parameters { get; init; }
}

[UsedImplicitly]
internal class UpdateHttpStepCommandHandler(
    IDbContextFactory<AppDbContext> dbContextFactory,
    StepValidator validator
) : UpdateStepCommandHandler<UpdateHttpStepCommand, HttpStep>(dbContextFactory, validator)
{
    protected override Task<HttpStep?> GetStepAsync(Guid stepId, AppDbContext dbContext,
        CancellationToken cancellationToken)
    {
        return dbContext.HttpSteps
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

    protected override async Task UpdateTypeSpecificPropertiesAsync(HttpStep step, UpdateHttpStepCommand request,
        AppDbContext dbContext, CancellationToken cancellationToken)
    {
        step.TimeoutMinutes = request.TimeoutMinutes;
        step.Url = request.Url;
        step.Method = request.Method;
        step.Body = request.Body;
        step.BodyFormat = request.BodyFormat;
        step.Headers.Clear();
        step.Headers.AddRange(request.Headers);
        step.DisableAsyncPattern = request.DisableAsyncPattern;
        
        await SynchronizeParametersAsync<HttpStepParameter, UpdateStepParameter>(
            step,
            request.Parameters,
            parameter => new HttpStepParameter
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