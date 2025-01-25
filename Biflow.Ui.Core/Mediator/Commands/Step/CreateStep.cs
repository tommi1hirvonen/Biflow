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
    public required IDictionary<Guid, DependencyType> Dependencies { get; init; }
    public required CreateExecutionConditionParameter[] ExecutionConditionParameters { get; init; }
    public required DataObjectRelation[] Sources { get; init; }
    public required DataObjectRelation[] Targets { get; init; }
}

public abstract class CreateStepCommandHandler<TCommand, TStep>(
    IDbContextFactory<AppDbContext> dbContextFactory,
    StepValidator validator
    ) : IRequestHandler<TCommand, TStep>
    where TCommand : CreateStepCommand<TStep>
    where TStep : Step
{
    protected abstract Task<TStep> CreateStepAsync(
        TCommand request, AppDbContext dbContext, CancellationToken cancellationToken);
    
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
        
        // Fetch dependency step ids from DB and check matching ids.
        var dependentStepIds = request.Dependencies.Keys.ToArray();
        var dependentStepIdsFromDb = await dbContext.Steps
            .Where(s => dependentStepIds.Contains(s.StepId))
            .Select(s => s.StepId)
            .ToArrayAsync(cancellationToken);
        foreach (var id in dependentStepIds)
        {
            if (!dependentStepIdsFromDb.Contains(id))
            {
                throw new NotFoundException<Step>(id);
            }
        }
        
        var dataObjectIds = request.Sources
            .Concat(request.Targets)
            .Select(x => x.DataObjectId)
            .Distinct()
            .ToArray();
        var dataObjects = await dbContext.DataObjects
            .Where(x => dataObjectIds.Contains(x.ObjectId))
            .ToArrayAsync(cancellationToken);
        foreach (var id in dataObjectIds)
        {
            if (dataObjects.All(x => x.ObjectId != id))
            {
                throw new NotFoundException<DataObject>(id);
            }
        }
        
        var step = await CreateStepAsync(request, dbContext, cancellationToken);
        
        foreach (var dependency in request.Dependencies)
            step.Dependencies.Add(new Dependency
            {
                StepId = step.StepId,
                DependantOnStepId = dependency.Key,
                DependencyType = dependency.Value
            });
        
        foreach (var tag in stepTags)
            step.Tags.Add(tag);

        foreach (var parameter in request.ExecutionConditionParameters)
            step.ExecutionConditionParameters.Add(new ExecutionConditionParameter
            {
               ParameterName = parameter.ParameterName,
               ParameterValue = parameter.ParameterValue,
               JobParameterId = parameter.InheritFromJobParameterId
            });
        
        var relations = request.Sources
            .Select(x => (DataObjectReferenceType.Source, x))
            .Concat(request.Targets.Select(x => (DataObjectReferenceType.Target, x)));
        foreach (var (referenceType, (dataObjectId, dataAttributes)) in relations)
        {
            var dataObject = dataObjects.First(x => x.ObjectId == dataObjectId);
            step.DataObjects.Add(new StepDataObject
            {
                StepId = step.StepId,
                ObjectId = dataObject.ObjectId,
                DataObject = dataObject,
                ReferenceType = referenceType,
                DataAttributes = dataAttributes.Distinct().Order().ToList()
            });
        }

        step.EnsureDataAnnotationsValidated();
        await validator.EnsureValidatedAsync(step, cancellationToken);
        
        dbContext.Steps.Add(step);
        await dbContext.SaveChangesAsync(cancellationToken);
        
        return step;
    }
}