namespace Biflow.Ui.Core;

public class UpdateDatabricksStepCommand : UpdateStepCommand<DatabricksStep>
{
    public required int TimeoutMinutes { get; init; }
    public required Guid DatabricksWorkspaceId { get; init; }
    public required DatabricksStepSettings DatabricksStepSettings { get; init; }
    public required UpdateStepParameter[] Parameters { get; init; }
}

[UsedImplicitly]
internal class UpdateDatabricksStepCommandHandler(
    IDbContextFactory<AppDbContext> dbContextFactory,
    StepValidator validator
) : UpdateStepCommandHandler<UpdateDatabricksStepCommand, DatabricksStep>(dbContextFactory, validator)
{
    protected override Task<DatabricksStep?> GetStepAsync(
        Guid stepId, AppDbContext dbContext, CancellationToken cancellationToken)
    {
        return dbContext.DatabricksSteps
            .Include(step => step.Job)
            .ThenInclude(job => job.JobParameters)
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
    
    protected override async Task UpdateTypeSpecificPropertiesAsync(
        DatabricksStep step, UpdateDatabricksStepCommand request, AppDbContext dbContext, CancellationToken cancellationToken)
    {
        // Check that the pipeline client exists.
        if (!await dbContext.DatabricksWorkspaces
                .AnyAsync(x => x.WorkspaceId == request.DatabricksWorkspaceId, cancellationToken))
        {
            throw new NotFoundException<DatabricksWorkspace>(request.DatabricksWorkspaceId);
        }
        
        step.TimeoutMinutes = request.TimeoutMinutes;
        step.DatabricksWorkspaceId = request.DatabricksWorkspaceId;
        step.DatabricksStepSettings = request.DatabricksStepSettings;
        
        await SynchronizeParametersAsync<DatabricksStepParameter, UpdateStepParameter>(
            step,
            request.Parameters,
            parameter => new DatabricksStepParameter
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