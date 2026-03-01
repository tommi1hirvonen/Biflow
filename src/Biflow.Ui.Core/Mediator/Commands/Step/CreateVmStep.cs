namespace Biflow.Ui.Core;

public class CreateVmStepCommand : CreateStepCommand<VmStep>
{
    public required Guid AzureCredentialId { get; init; }
    public required string VirtualMachineResourceId { get; init; }
    public required VmStepOperation Operation { get; init; }
}

[UsedImplicitly]
internal class CreateVmStepCommandHandler(
    IDbContextFactory<AppDbContext> dbContextFactory,
    StepValidator validator)
    : CreateStepCommandHandler<CreateVmStepCommand, VmStep>(dbContextFactory, validator)
{
    protected override async Task<VmStep> CreateStepAsync(
        CreateVmStepCommand request, AppDbContext dbContext, CancellationToken cancellationToken)
    {
        if (!await dbContext.AzureCredentials
                .AnyAsync(x => x.AzureCredentialId == request.AzureCredentialId, cancellationToken))
        {
            throw new NotFoundException<AzureCredential>(request.AzureCredentialId);
        }

        return new VmStep
        {
            JobId = request.JobId,
            StepName = request.StepName,
            StepDescription = request.StepDescription,
            ExecutionPhase = request.ExecutionPhase,
            DuplicateExecutionBehaviour = request.DuplicateExecutionBehaviour,
            IsEnabled = request.IsEnabled,
            RetryAttempts = request.RetryAttempts,
            RetryIntervalMinutes = request.RetryIntervalMinutes,
            ExecutionConditionExpression = new EvaluationExpression
                { Expression = request.ExecutionConditionExpression },
            AzureCredentialId = request.AzureCredentialId,
            VirtualMachineResourceId = request.VirtualMachineResourceId,
            Operation = request.Operation
        };
    }
}
