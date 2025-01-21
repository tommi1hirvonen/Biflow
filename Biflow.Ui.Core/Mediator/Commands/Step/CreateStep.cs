namespace Biflow.Ui.Core;

public abstract class CreateStepCommand<TStep> : IRequest<TStep> where TStep : Step
{
    public required Guid JobId { get; init; }
    public required string StepName { get; init; }
    public required string? StepDescription { get; init; }
    public required int ExecutionPhase { get; init; }
    public required DuplicateExecutionBehaviour DuplicateExecutionBehaviour { get; init; }
    public required bool IsEnabled { get; init; }
    public required int RetryAttempts { get; init; }
    public required double RetryIntervalMinutes { get; init; }
    public required string? ExecutionConditionExpression { get; init; }
    public required Guid[] StepTagIds { get; init; }
}

public abstract class CreateStepCommandHandler<TCommand, TStep>(
    IDbContextFactory<AppDbContext> dbContextFactory,
    StepValidator validator
    ) : IRequestHandler<TCommand, TStep>
    where TCommand : CreateStepCommand<TStep>
    where TStep : Step
{
    protected abstract TStep CreateStep(TCommand request);
    
    public async Task<TStep> Handle(TCommand request, CancellationToken cancellationToken)
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
        
        var step = CreateStep(request);
        
        foreach (var tag in stepTags) step.Tags.Add(tag);
        
        step.EnsureDataAnnotationsValidated();
        validator.EnsureValidated(step);
        
        dbContext.Steps.Add(step);
        await dbContext.SaveChangesAsync(cancellationToken);
        
        return step;
    }
}