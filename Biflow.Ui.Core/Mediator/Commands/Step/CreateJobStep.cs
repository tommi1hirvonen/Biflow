namespace Biflow.Ui.Core;

public class CreateJobStepCommand : CreateStepCommand<JobStep>
{
    public required double TimeoutMinutes { get; init; }
    public required Guid JobToExecuteId { get; init; }
    public required bool ExecuteSynchronized { get; init; }
    public required Guid[] FilterStepTagIds { get; init; }
    public required CreateJobStepParameter[] Parameters { get; init; }
}

[UsedImplicitly]
internal class CreateJobStepCommandHandler(
    IDbContextFactory<AppDbContext> dbContextFactory,
    StepValidator validator
) : CreateStepCommandHandler<CreateJobStepCommand, JobStep>(dbContextFactory, validator)
{
    protected override async Task<JobStep> CreateStepAsync(
        CreateJobStepCommand request, AppDbContext dbContext, CancellationToken cancellationToken)
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
        
        var step = new JobStep
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
            TimeoutMinutes = request.TimeoutMinutes,
            JobToExecuteId = request.JobToExecuteId,
            JobExecuteSynchronized = request.ExecuteSynchronized
        };
        
        foreach (var tag in stepTags)
            step.TagFilters.Add(tag);
        
        foreach (var createParameter in request.Parameters)
        {
            var jobParameter = jobParameters.First(p => p.ParameterId == createParameter.AssignToJobParameterId);
            var parameter = new JobStepParameter(request.JobToExecuteId)
            {
                AssignToJobParameterId = createParameter.AssignToJobParameterId,
                ParameterName = jobParameter.ParameterName,
                ParameterValue = createParameter.ParameterValue,
                UseExpression = createParameter.UseExpression,
                Expression = new EvaluationExpression { Expression = createParameter.Expression },
                InheritFromJobParameterId = createParameter.InheritFromJobParameterId
            };
            foreach (var createExpressionParameter in createParameter.ExpressionParameters)
            {
                parameter.AddExpressionParameter(
                    createExpressionParameter.ParameterName,
                    createExpressionParameter.InheritFromJobParameterId);
            }
            step.StepParameters.Add(parameter);
        }

        return step;
    }
}