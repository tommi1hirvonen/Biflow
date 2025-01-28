namespace Biflow.Ui.Core;

public class UpdateFunctionStepCommand : UpdateStepCommand<FunctionStep>
{
    public required double TimeoutMinutes { get; init; }
    public required Guid FunctionAppId { get; init; }
    public required string FunctionUrl { get; init; }
    public required string? FunctionInput { get; init; }
    public required bool FunctionIsDurable { get; init; }
    public required string? FunctionKey { get; init; }
    public required UpdateStepParameter[] Parameters { get; init; }
}

[UsedImplicitly]
internal class UpdateFunctionStepCommandHandler(
    IDbContextFactory<AppDbContext> dbContextFactory,
    StepValidator validator
) : UpdateStepCommandHandler<UpdateFunctionStepCommand, FunctionStep>(dbContextFactory, validator)
{
    protected override Task<FunctionStep?> GetStepAsync(
        Guid stepId, AppDbContext dbContext, CancellationToken cancellationToken)
    {
        return dbContext.FunctionSteps
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
        FunctionStep step, UpdateFunctionStepCommand request, AppDbContext dbContext, CancellationToken cancellationToken)
    {
        // Check that the function app exists.
        if (!await dbContext.FunctionApps.AnyAsync(x => x.FunctionAppId == request.FunctionAppId, cancellationToken))
        {
            throw new NotFoundException<FunctionApp>(request.FunctionAppId);
        }
        
        step.TimeoutMinutes = request.TimeoutMinutes;
        step.FunctionUrl = request.FunctionUrl;
        step.FunctionInput = request.FunctionInput;
        step.FunctionIsDurable = request.FunctionIsDurable;
        step.FunctionKey = request.FunctionKey;
        
        await SynchronizeParametersAsync<FunctionStepParameter, UpdateStepParameter>(
            step,
            request.Parameters,
            parameter => new FunctionStepParameter
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