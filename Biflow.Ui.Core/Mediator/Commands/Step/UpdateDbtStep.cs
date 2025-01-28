namespace Biflow.Ui.Core;

public class UpdateDbtStepCommand : UpdateStepCommand<DbtStep>
{
    public required double TimeoutMinutes { get; init; }
    public required Guid DbtAccountId { get; init; }
    public required DbtJobDetails DbtJob { get; init; }
}

[UsedImplicitly]
internal class UpdateDbtStepCommandHandler(
    IDbContextFactory<AppDbContext> dbContextFactory,
    StepValidator validator
) : UpdateStepCommandHandler<UpdateDbtStepCommand, DbtStep>(dbContextFactory, validator)
{
    protected override Task<DbtStep?> GetStepAsync(
        Guid stepId, AppDbContext dbContext, CancellationToken cancellationToken)
    {
        return dbContext.DbtSteps
            .Include(step => step.Tags)
            .Include(step => step.Dependencies)
            .Include(step => step.DataObjects)
            .ThenInclude(s => s.DataObject)
            .Include(step => step.ExecutionConditionParameters)
            .FirstOrDefaultAsync(step => step.StepId == stepId, cancellationToken);
    }
    
    protected override async Task UpdateTypeSpecificPropertiesAsync(
        DbtStep step, UpdateDbtStepCommand request, AppDbContext dbContext, CancellationToken cancellationToken)
    {
        // Check that the Azure credential exists.
        if (!await dbContext.DbtAccounts
                .AnyAsync(x => x.DbtAccountId == request.DbtAccountId, cancellationToken))
        {
            throw new NotFoundException<DbtAccount>(request.DbtAccountId);
        }
        
        step.TimeoutMinutes = request.TimeoutMinutes;
        step.DbtAccountId = request.DbtAccountId;
        step.DbtJob = request.DbtJob;
    }
}