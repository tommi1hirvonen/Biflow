namespace Biflow.Ui.Core;

public class UpdateDatasetStepCommand : UpdateStepCommand<DatasetStep>
{
    public required Guid FabricWorkspaceId { get; init; }
    public required Guid DatasetId { get; init; }
    public required string? DatasetName { get; init; }
}

[UsedImplicitly]
internal class UpdateDatasetStepCommandHandler(
    IDbContextFactory<AppDbContext> dbContextFactory,
    StepValidator validator
) : UpdateStepCommandHandler<UpdateDatasetStepCommand, DatasetStep>(dbContextFactory, validator)
{
    protected override Task<DatasetStep?> GetStepAsync(
        Guid stepId, AppDbContext dbContext, CancellationToken cancellationToken)
    {
        return dbContext.DatasetSteps
            .Include(step => step.Tags)
            .Include(step => step.Dependencies)
            .Include(step => step.DataObjects)
            .ThenInclude(s => s.DataObject)
            .Include(step => step.ExecutionConditionParameters)
            .FirstOrDefaultAsync(step => step.StepId == stepId, cancellationToken);
    }
    
    protected override async Task UpdateTypeSpecificPropertiesAsync(DatasetStep step, UpdateDatasetStepCommand request,
        AppDbContext dbContext, CancellationToken cancellationToken)
    {
        // Check that the Fabric workspace exists.
        if (!await dbContext.FabricWorkspaces
                .AnyAsync(x => x.FabricWorkspaceId == request.FabricWorkspaceId, cancellationToken))
        {
            throw new NotFoundException<FabricWorkspace>(request.FabricWorkspaceId);
        }
        
        step.FabricWorkspaceId = request.FabricWorkspaceId;
        step.DatasetId = request.DatasetId.ToString();
        step.DatasetName = request.DatasetName;
    }
}