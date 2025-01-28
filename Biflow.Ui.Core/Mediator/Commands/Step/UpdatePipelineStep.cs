namespace Biflow.Ui.Core;

public class UpdatePipelineStepCommand : UpdateStepCommand<PipelineStep>
{
    public required double TimeoutMinutes { get; init; }
    public required Guid PipelineClientId { get; init; }
    public required string PipelineName { get; init; } 
    public required UpdateStepParameter[] Parameters { get; init; }
}

[UsedImplicitly]
internal class UpdatePipelineStepCommandHandler(
    IDbContextFactory<AppDbContext> dbContextFactory,
    StepValidator validator
) : UpdateStepCommandHandler<UpdatePipelineStepCommand, PipelineStep>(dbContextFactory, validator)
{
    protected override Task<PipelineStep?> GetStepAsync(
        Guid stepId, AppDbContext dbContext, CancellationToken cancellationToken)
    {
        return dbContext.PipelineSteps
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
        PipelineStep step, UpdatePipelineStepCommand request, AppDbContext dbContext, CancellationToken cancellationToken)
    {
        // Check that the pipeline client exists.
        if (!await dbContext.PipelineClients.AnyAsync(x => x.PipelineClientId == request.PipelineClientId, cancellationToken))
        {
            throw new NotFoundException<PipelineClient>(request.PipelineClientId);
        }
        
        step.TimeoutMinutes = request.TimeoutMinutes;
        step.PipelineClientId = request.PipelineClientId;
        step.PipelineName = request.PipelineName;
        
        await SynchronizeParametersAsync<PipelineStepParameter, UpdateStepParameter>(
            step,
            request.Parameters,
            parameter => new PipelineStepParameter
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