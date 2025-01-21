namespace Biflow.Ui.Core;

public abstract class UpdateStepCommand<TStep> : IRequest<TStep> where TStep : Step
{
    public required Guid StepId { get; init; }
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

public abstract class UpdateStepCommandHandler<TCommand, TStep>(
    IDbContextFactory<AppDbContext> dbContextFactory,
    StepValidator validator
) : IRequestHandler<TCommand, TStep>
    where TCommand : UpdateStepCommand<TStep>
    where TStep : Step
{
    protected abstract Task UpdatePropertiesAsync(
        TStep step, TCommand request, AppDbContext dbContext, CancellationToken cancellationToken);
    
    public async Task<TStep> Handle(TCommand request, CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        
        var step = await dbContext.Set<TStep>()
            .Include(s => s.Tags)
            .FirstOrDefaultAsync(s => s.StepId == request.StepId, cancellationToken)
            ?? throw new NotFoundException<TStep>(request.StepId);
        
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
        
        step.StepName = request.StepName;
        step.StepDescription = request.StepDescription;
        step.ExecutionPhase = request.ExecutionPhase;
        step.DuplicateExecutionBehaviour = request.DuplicateExecutionBehaviour;
        step.IsEnabled = request.IsEnabled;
        step.RetryAttempts = request.RetryAttempts;
        step.RetryIntervalMinutes = request.RetryIntervalMinutes;
        step.ExecutionConditionExpression.Expression = request.ExecutionConditionExpression;
        
        await UpdatePropertiesAsync(step, request, dbContext, cancellationToken);
        
        // Synchronize tags
        var stepTagsToAdd = stepTags.Where(t1 => step.Tags.All(t2 => t2.TagId != t1.TagId));
        foreach (var tag in stepTagsToAdd)
        {
            step.Tags.Add(tag);
        }
        var stepTagsToRemove = step.Tags
            .Where(t => !request.StepTagIds.Contains(t.TagId))
            .ToArray(); // Materialize results because items may be removed from the sequence during iteration.
        foreach (var tag in stepTagsToRemove)
        {
            step.Tags.Remove(tag);
        }
        
        step.EnsureDataAnnotationsValidated();
        validator.EnsureValidated(step);
        
        await dbContext.SaveChangesAsync(cancellationToken);

        return step;
    }
}