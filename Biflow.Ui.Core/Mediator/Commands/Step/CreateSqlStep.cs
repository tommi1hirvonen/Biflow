namespace Biflow.Ui.Core;

public record CreateSqlStepCommand(
    Guid JobId,
    string StepName,
    string? StepDescription,
    int ExecutionPhase,
    DuplicateExecutionBehaviour DuplicateExecutionBehaviour,
    bool IsEnabled,
    int RetryAttempts,
    int RetryIntervalMinutes,
    string? ExecutionConditionExpression,
    Guid[] StepTagIds,
    int TimeoutMinutes,
    string SqlStatement,
    Guid ConnectionId,
    Guid? ResultCaptureJobParameterId) : IRequest<SqlStep>;

[UsedImplicitly]
internal class CreateSqlStepCommandHandler(
    IDbContextFactory<AppDbContext> dbContextFactory,
    StepValidator validator) : IRequestHandler<CreateSqlStepCommand, SqlStep>
{
    public async Task<SqlStep> Handle(CreateSqlStepCommand request, CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        
        if (!await dbContext.Jobs.AnyAsync(j => j.JobId == request.JobId, cancellationToken))
        {
            throw new NotFoundException<Job>(request.JobId);
        }
        
        var stepTags = await dbContext.StepTags
            .Where(t => request.StepTagIds.Contains(t.TagId))
            .ToArrayAsync(cancellationToken);

        foreach (var id in request.StepTagIds)
        {
            if (stepTags.All(t => t.TagId != id))
            {
                throw new NotFoundException<StepTag>(id);
            }
        }
        
        var step = new SqlStep
        {
            JobId = request.JobId,
            StepName = request.StepName,
            StepDescription = request.StepDescription,
            ExecutionPhase = request.ExecutionPhase,
            DuplicateExecutionBehaviour = request.DuplicateExecutionBehaviour,
            IsEnabled = request.IsEnabled,
            RetryAttempts = request.RetryAttempts,
            RetryIntervalMinutes = request.RetryIntervalMinutes,
            ExecutionConditionExpression = new EvaluationExpression { Expression = request.ExecutionConditionExpression },
            TimeoutMinutes = request.TimeoutMinutes,
            SqlStatement = request.SqlStatement,
            ConnectionId = request.ConnectionId,
            ResultCaptureJobParameterId = request.ResultCaptureJobParameterId
        };
        
        foreach (var tag in stepTags) step.Tags.Add(tag);
        
        step.EnsureDataAnnotationsValidated();
        validator.EnsureValidated(step);
        
        dbContext.SqlSteps.Add(step);
        await dbContext.SaveChangesAsync(cancellationToken);
        
        return step;
    }
}