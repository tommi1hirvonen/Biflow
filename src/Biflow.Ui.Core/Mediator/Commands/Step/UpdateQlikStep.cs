namespace Biflow.Ui.Core;

public class UpdateQlikStepCommand : UpdateStepCommand<QlikStep>
{
    public required double TimeoutMinutes { get; init; }
    public required Guid QlikCloudEnvironmentId { get; init; }
    public required QlikStepSettings Settings { get; init; }
}

[UsedImplicitly]
internal class UpdateQlikStepCommandHandler(
    IDbContextFactory<AppDbContext> dbContextFactory,
    StepValidator validator
) : UpdateStepCommandHandler<UpdateQlikStepCommand, QlikStep>(dbContextFactory, validator)
{
    protected override Task<QlikStep?> GetStepAsync(
        Guid stepId, AppDbContext dbContext, CancellationToken cancellationToken)
    {
        return dbContext.QlikSteps
            .Include(step => step.Tags)
            .Include(step => step.Dependencies)
            .Include(step => step.DataObjects)
            .ThenInclude(s => s.DataObject)
            .Include(step => step.ExecutionConditionParameters)
            .FirstOrDefaultAsync(step => step.StepId == stepId, cancellationToken);
    }
    
    protected override async Task UpdateTypeSpecificPropertiesAsync(QlikStep step, UpdateQlikStepCommand request,
        AppDbContext dbContext, CancellationToken cancellationToken)
    {
        // Check that the Qlik Cloud environment exists.
        if (!await dbContext.QlikCloudEnvironments
                .AnyAsync(x => x.QlikCloudEnvironmentId == request.QlikCloudEnvironmentId, cancellationToken))
        {
            throw new NotFoundException<QlikCloudEnvironment>(request.QlikCloudEnvironmentId);
        }
        
        step.TimeoutMinutes = request.TimeoutMinutes;
        step.QlikCloudEnvironmentId = request.QlikCloudEnvironmentId;
        step.QlikStepSettings = request.Settings;
    }
}