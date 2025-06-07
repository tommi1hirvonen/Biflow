namespace Biflow.Ui.Core;

public class UpdateScdStepCommand : UpdateStepCommand<ScdStep>
{
    public required double TimeoutMinutes { get; init; }
    public required Guid ScdTableId { get; init; }
}

[UsedImplicitly]
internal class UpdateScdStepCommandHandler(
    IDbContextFactory<AppDbContext> dbContextFactory,
    StepValidator validator
) : UpdateStepCommandHandler<UpdateScdStepCommand, ScdStep>(dbContextFactory, validator)
{
    protected override Task<ScdStep?> GetStepAsync(
        Guid stepId, AppDbContext dbContext, CancellationToken cancellationToken)
    {
        return dbContext.ScdSteps
            .Include(step => step.Tags)
            .Include(step => step.Dependencies)
            .Include(step => step.DataObjects)
            .ThenInclude(s => s.DataObject)
            .Include(step => step.ExecutionConditionParameters)
            .FirstOrDefaultAsync(step => step.StepId == stepId, cancellationToken);
    }
    
    protected override async Task UpdateTypeSpecificPropertiesAsync(ScdStep step, UpdateScdStepCommand request,
        AppDbContext dbContext, CancellationToken cancellationToken)
    {
        // Check that the SCD table exists.
        if (!await dbContext.ScdTables.AnyAsync(x => x.ScdTableId == request.ScdTableId, cancellationToken))
        {
            throw new NotFoundException<ScdTable>(request.ScdTableId);
        }
        
        step.TimeoutMinutes = request.TimeoutMinutes;
        step.ScdTableId = request.ScdTableId;
    }
}