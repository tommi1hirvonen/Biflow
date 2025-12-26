namespace Biflow.Ui.Core;

public class UpdateFabricStepCommand : UpdateStepCommand<FabricStep>
{
    public required double TimeoutMinutes { get; init; }
    public required FabricItemType ItemType { get; init; }
    public required Guid ItemId { get; init; }
    public required string ItemName { get; init; }
    public required Guid FabricWorkspaceId { get; init; }
    public required UpdateStepParameter[] Parameters { get; init; }
}

[UsedImplicitly]
internal class UpdateFabricStepCommandHandler(
    IDbContextFactory<AppDbContext> dbContextFactory,
    StepValidator validator
) : UpdateStepCommandHandler<UpdateFabricStepCommand, FabricStep>(dbContextFactory, validator)
{
    protected override Task<FabricStep?> GetStepAsync(
        Guid stepId, AppDbContext dbContext, CancellationToken cancellationToken)
    {
        return dbContext.FabricSteps
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
    
    protected override async Task UpdateTypeSpecificPropertiesAsync(FabricStep step, UpdateFabricStepCommand request,
        AppDbContext dbContext, CancellationToken cancellationToken)
    {
        // Check that the Fabric workspace exists.
        if (!await dbContext.FabricWorkspaces
                .AnyAsync(x => x.FabricWorkspaceId == request.FabricWorkspaceId, cancellationToken))
        {
            throw new NotFoundException<FabricWorkspace>(request.FabricWorkspaceId);
        }
        
        step.TimeoutMinutes = request.TimeoutMinutes;
        step.ItemType = request.ItemType;
        step.ItemId = request.ItemId;
        step.ItemName = request.ItemName;
        step.FabricWorkspaceId = request.FabricWorkspaceId;
        
        await SynchronizeParametersAsync<FabricStepParameter, UpdateStepParameter>(
            step,
            request.Parameters,
            parameter => new FabricStepParameter
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