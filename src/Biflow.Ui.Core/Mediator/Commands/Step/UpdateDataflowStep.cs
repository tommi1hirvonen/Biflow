namespace Biflow.Ui.Core;

public class UpdateDataflowStepCommand : UpdateStepCommand<DataflowStep>
{
    public required double TimeoutMinutes { get; init; }
    public required Guid FabricWorkspaceId { get; init; }
    public required Guid DataflowId { get; init; }
    public required string? DataflowName { get; init; }
}

[UsedImplicitly]
internal class UpdateDataflowStepCommandHandler(
    IDbContextFactory<AppDbContext> dbContextFactory,
    StepValidator validator
) : UpdateStepCommandHandler<UpdateDataflowStepCommand, DataflowStep>(dbContextFactory, validator)
{
    protected override Task<DataflowStep?> GetStepAsync(
        Guid stepId, AppDbContext dbContext, CancellationToken cancellationToken)
    {
        return dbContext.DataflowSteps
            .Include(step => step.Tags)
            .Include(step => step.Dependencies)
            .Include(step => step.DataObjects)
            .ThenInclude(s => s.DataObject)
            .Include(step => step.ExecutionConditionParameters)
            .FirstOrDefaultAsync(step => step.StepId == stepId, cancellationToken);
    }
    
    protected override async Task UpdateTypeSpecificPropertiesAsync(
        DataflowStep step,
        UpdateDataflowStepCommand request,
        AppDbContext dbContext,
        CancellationToken cancellationToken)
    {
        // Check that the Fabric workspace exists.
        if (!await dbContext.FabricWorkspaces
                .AnyAsync(x => x.FabricWorkspaceId == request.FabricWorkspaceId, cancellationToken))
        {
            throw new NotFoundException<FabricWorkspace>(request.FabricWorkspaceId);
        }
        
        step.TimeoutMinutes = request.TimeoutMinutes;
        step.FabricWorkspaceId = request.FabricWorkspaceId;
        step.DataflowId = request.DataflowId.ToString();
        step.DataflowName = request.DataflowName;
    }
}