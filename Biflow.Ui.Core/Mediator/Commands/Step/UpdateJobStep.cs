namespace Biflow.Ui.Core;

public class UpdateJobStepCommand : UpdateStepCommand<JobStep>
{
    public required double TimeoutMinutes { get; init; }
    public required Guid JobToExecuteId { get; init; }
    public required bool ExecuteSynchronized { get; init; }
    public required Guid[] FilterStepTagIds { get; init; }
    public required UpdateJobStepParameter[] Parameters { get; init; }
}

[UsedImplicitly]
internal class UpdateJobStepCommandHandler(
    IDbContextFactory<AppDbContext> dbContextFactory,
    StepValidator validator
) : UpdateStepCommandHandler<UpdateJobStepCommand, JobStep>(dbContextFactory, validator)
{
    protected override Task<JobStep?> GetStepAsync(
        Guid stepId, AppDbContext dbContext, CancellationToken cancellationToken)
    {
        return dbContext.JobSteps
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
        JobStep step,
        UpdateJobStepCommand request,
        AppDbContext dbContext,
        CancellationToken cancellationToken)
    {
        // Check that the job to execute exists.
        if (!await dbContext.Jobs
                .AnyAsync(x => x.JobId == request.JobToExecuteId, cancellationToken))
        {
            throw new NotFoundException<Job>(request.JobToExecuteId);
        }
        
        var stepTags = await dbContext.StepTags
            .Where(t => request.FilterStepTagIds.Contains(t.TagId))
            .ToArrayAsync(cancellationToken);
        
        foreach (var id in request.FilterStepTagIds)
        {
            if (stepTags.All(t => t.TagId != id))
            {
                throw new NotFoundException<StepTag>(id);
            }
        }
        
        var jobParameterIds = request.Parameters
            .Select(p => p.AssignToJobParameterId)
            .ToArray();
        var jobParameters = await dbContext.Set<JobParameter>()
            .Where(p => p.JobId == request.JobToExecuteId && jobParameterIds.Contains(p.ParameterId))
            .ToArrayAsync(cancellationToken);

        foreach (var id in jobParameterIds)
        {
            if (jobParameters.All(t => t.ParameterId != id))
            {
                throw new NotFoundException<JobParameter>(("JobId", request.JobToExecuteId), ("ParameterId", id));
            }
        }
        
        step.TimeoutMinutes = request.TimeoutMinutes;
        step.JobToExecuteId = request.JobToExecuteId;
        step.JobExecuteSynchronized = request.ExecuteSynchronized;
        
        // Synchronize filter step tags
        var stepTagsToAdd = stepTags.Where(t1 => step.TagFilters.All(t2 => t2.TagId != t1.TagId));
        foreach (var tag in stepTagsToAdd)
        {
            step.TagFilters.Add(tag);
        }
        var stepTagsToRemove = step.TagFilters
            .Where(t => !request.FilterStepTagIds.Contains(t.TagId))
            .ToArray(); // materialize since items may be removed from the sequence during iteration
        foreach (var tag in stepTagsToRemove)
        {
            step.TagFilters.Remove(tag);
        }
        
        await SynchronizeParametersAsync<JobStepParameter, UpdateJobStepParameter>(
            step,
            request.Parameters,
            parameter => new JobStepParameter(request.JobToExecuteId)
            {
                AssignToJobParameterId = jobParameters
                    .First(p => p.ParameterId == parameter.AssignToJobParameterId).ParameterId,
                ParameterName = parameter.ParameterName,
                ParameterValue = parameter.ParameterValue,
                UseExpression = parameter.UseExpression,
                Expression = new EvaluationExpression { Expression = parameter.Expression },
                InheritFromJobParameterId = parameter.InheritFromJobParameterId
            },
            dbContext,
            cancellationToken);
        
        // Update AssignToJobParameterId for matching parameters as SynchronizeParameters() does not handle that.
        foreach (var parameter in step.StepParameters)
        {
            var updateParameter = request.Parameters
                .FirstOrDefault(p => p.ParameterId == parameter.ParameterId);
            if (updateParameter is null) continue;
            var jobParameter = jobParameters.First(p => p.ParameterId == parameter.AssignToJobParameterId);
            parameter.ParameterName = jobParameter.ParameterName;
            parameter.AssignToJobParameterId = updateParameter.AssignToJobParameterId;
        }
    }
}