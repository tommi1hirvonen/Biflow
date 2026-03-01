namespace Biflow.Ui.Core;

public class UpdateWaitStepCommand : UpdateStepCommand<WaitStep>
{
    public required int WaitSeconds { get; init; }
}

[UsedImplicitly]
internal class UpdateWaitStepCommandHandler(
    IDbContextFactory<AppDbContext> dbContextFactory,
    StepValidator validator
) : UpdateStepCommandHandler<UpdateWaitStepCommand, WaitStep>(dbContextFactory, validator)
{
    protected override Task<WaitStep?> GetStepAsync(Guid stepId, AppDbContext dbContext,
        CancellationToken cancellationToken)
    {
        return dbContext.WaitSteps
            .Include(step => step.Tags)
            .Include(step => step.Dependencies)
            .Include(step => step.DataObjects)
            .ThenInclude(s => s.DataObject)
            .Include(step => step.ExecutionConditionParameters)
            .FirstOrDefaultAsync(step => step.StepId == stepId, cancellationToken);
    }

    protected override Task UpdateTypeSpecificPropertiesAsync(WaitStep step, UpdateWaitStepCommand request,
        AppDbContext dbContext, CancellationToken cancellationToken)
    {
        step.WaitSeconds = request.WaitSeconds;
        return Task.CompletedTask;
    }
}