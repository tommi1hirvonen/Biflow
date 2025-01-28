namespace Biflow.Ui.Core;

public class UpdateFabricStepCommand : UpdateStepCommand<FabricStep>
{
    public required double TimeoutMinutes { get; init; }
    public required Guid WorkspaceId { get; init; }
    public required FabricItemType ItemType { get; init; }
    public required Guid ItemId { get; init; }
    public required Guid AzureCredentialId { get; init; }
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
        // Check that the Azure credential exists.
        if (!await dbContext.AzureCredentials
                .AnyAsync(x => x.AzureCredentialId == request.AzureCredentialId, cancellationToken))
        {
            throw new NotFoundException<AzureCredential>(request.AzureCredentialId);
        }
        
        step.TimeoutMinutes = request.TimeoutMinutes;
        step.WorkspaceId = request.WorkspaceId;
        step.ItemType = request.ItemType;
        step.ItemId = request.ItemId;
        step.AzureCredentialId = request.AzureCredentialId;
        
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