namespace Biflow.Ui.Core;

public class UpdateVmStepCommand : UpdateStepCommand<VmStep>
{
    public required Guid AzureCredentialId { get; init; }
    public required string VirtualMachineResourceId { get; init; }
    public required VmStepOperation Operation { get; init; }
}

[UsedImplicitly]
internal class UpdateVmStepCommandHandler(
    IDbContextFactory<AppDbContext> dbContextFactory,
    StepValidator validator
) : UpdateStepCommandHandler<UpdateVmStepCommand, VmStep>(dbContextFactory, validator)
{
    protected override Task<VmStep?> GetStepAsync(Guid stepId, AppDbContext dbContext,
        CancellationToken cancellationToken)
    {
        return dbContext.VmSteps
            .Include(step => step.Tags)
            .Include(step => step.Dependencies)
            .Include(step => step.DataObjects)
            .ThenInclude(s => s.DataObject)
            .Include(step => step.ExecutionConditionParameters)
            .FirstOrDefaultAsync(step => step.StepId == stepId, cancellationToken);
    }

    protected override async Task UpdateTypeSpecificPropertiesAsync(VmStep step, UpdateVmStepCommand request,
        AppDbContext dbContext, CancellationToken cancellationToken)
    {
        if (!await dbContext.AzureCredentials
                .AnyAsync(x => x.AzureCredentialId == request.AzureCredentialId, cancellationToken))
        {
            throw new NotFoundException<AzureCredential>(request.AzureCredentialId);
        }

        step.AzureCredentialId = request.AzureCredentialId;
        step.VirtualMachineResourceId = request.VirtualMachineResourceId;
        step.Operation = request.Operation;
    }
}
