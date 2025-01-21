namespace Biflow.Ui.Core;

public abstract class CreateStepCommandHandler<TCommand, TStep>(
    IDbContextFactory<AppDbContext> dbContextFactory,
    StepValidator validator
    ) : IRequestHandler<TCommand, TStep>
    where TCommand : IRequest<TStep>
    where TStep : Step
{
    protected abstract Guid GetJobId(TCommand request);
    
    protected abstract Guid[] GetStepTagIds(TCommand request);
    
    protected abstract TStep CreateStep(TCommand request);
    
    public async Task<TStep> Handle(TCommand request, CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        var jobId = GetJobId(request);
        var stepTagIds = GetStepTagIds(request);
        
        if (!await dbContext.Jobs.AnyAsync(j => j.JobId == jobId, cancellationToken))
        {
            throw new NotFoundException<Job>(jobId);
        }
        
        var stepTags = await dbContext.StepTags
            .Where(t => stepTagIds.Contains(t.TagId))
            .ToArrayAsync(cancellationToken);

        foreach (var id in stepTagIds)
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