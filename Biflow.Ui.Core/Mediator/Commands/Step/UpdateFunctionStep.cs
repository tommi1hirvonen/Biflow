namespace Biflow.Ui.Core;

/// <summary>
/// Command to fully update a function step.
/// The previous FunctionKey value can optionally be retained.
/// </summary>
public class UpdateFunctionStepCommand : UpdateStepCommand<FunctionStep>
{
    public required double TimeoutMinutes { get; init; }
    public required Guid? FunctionAppId { get; init; }
    public required string FunctionUrl { get; init; }
    public required string? FunctionInput { get; init; }
    public required FunctionInputFormat FunctionInputFormat { get; init; }
    public required bool FunctionIsDurable { get; init; }
    /// <summary>
    /// If null, the previous FunctionKey value will be retained.
    /// If empty string, the FunctionKey value will be cleared.
    /// </summary>
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
        FunctionStep step,
        UpdateFunctionStepCommand request,
        AppDbContext dbContext,
        CancellationToken cancellationToken)
    {
        // Check that the function app exists.
        if (request.FunctionAppId is { } id &&
            !await dbContext.FunctionApps.AnyAsync(x => x.FunctionAppId == id, cancellationToken))
        {
            throw new NotFoundException<FunctionApp>(id);
        }
        
        step.TimeoutMinutes = request.TimeoutMinutes;
        step.FunctionAppId = request.FunctionAppId;
        step.FunctionUrl = request.FunctionUrl;
        step.FunctionInput = request.FunctionInput;
        step.FunctionInputFormat = request.FunctionInputFormat;
        step.FunctionIsDurable = request.FunctionIsDurable;
        if (request.FunctionKey is not null)
        {
            step.FunctionKey = request.FunctionKey;
        }
        
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