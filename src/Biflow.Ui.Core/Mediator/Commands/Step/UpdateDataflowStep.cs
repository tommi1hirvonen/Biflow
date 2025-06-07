namespace Biflow.Ui.Core;

public class UpdateDataflowStepCommand : UpdateStepCommand<DataflowStep>
{
    public required double TimeoutMinutes { get; init; }
    public required Guid AzureCredentialId { get; init; }
    public required Guid WorkspaceId { get; init; }
    public required string? WorkspaceName { get; init; }
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
        // Check that the Azure credential exists.
        if (!await dbContext.AzureCredentials
                .AnyAsync(x => x.AzureCredentialId == request.AzureCredentialId, cancellationToken))
        {
            throw new NotFoundException<AzureCredential>(request.AzureCredentialId);
        }
        
        step.TimeoutMinutes = request.TimeoutMinutes;
        step.WorkspaceId = request.WorkspaceId.ToString();
        step.WorkspaceName = request.WorkspaceName;
        step.DataflowId = request.DataflowId.ToString();
        step.DataflowName = request.DataflowName;
    }
}