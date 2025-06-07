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
        
        var step = await CreateStepAsync(request, dbContext, cancellationToken);
        
        await AddDependenciesAsync(step, request.Dependencies, dbContext, cancellationToken);
        await AddTagsAsync(step, request.StepTagIds, dbContext, cancellationToken);
        await AddDataObjectsAsync(step, request.Sources, request.Targets, dbContext, cancellationToken);
        await AddExecutionConditionParametersAsync(step, request.ExecutionConditionParameters, dbContext,
            cancellationToken);

        step.EnsureDataAnnotationsValidated();
        await validator.EnsureValidatedAsync(step, cancellationToken);
        
        dbContext.Steps.Add(step);
        await dbContext.SaveChangesAsync(cancellationToken);
        
        return step;
    }

    private static async Task AddTagsAsync(TStep step, Guid[] stepTagIds, AppDbContext dbContext,
        CancellationToken cancellationToken)
    {
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

        foreach (var tag in stepTags)
        {
            step.Tags.Add(tag);
        }
    }

    private static async Task AddDependenciesAsync(TStep step, IDictionary<Guid, DependencyType> dependencies,
        AppDbContext dbContext, CancellationToken cancellationToken)
    {
        // Fetch dependency step ids from DB and check matching ids.
        var dependentStepIds = dependencies.Keys.ToArray();
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

        foreach (var dependency in dependencies)
        {
            step.Dependencies.Add(new Dependency
            {
                StepId = step.StepId,
                DependantOnStepId = dependency.Key,
                DependencyType = dependency.Value
            });
        }
    }

    private static async Task AddDataObjectsAsync(
        TStep step,
        DataObjectRelation[] sources,
        DataObjectRelation[] targets,
        AppDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var dataObjectIds = sources
            .Concat(targets)
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
        
        var relations = sources
            .Select(x => (DataObjectReferenceType.Source, x))
            .Concat(targets.Select(x => (DataObjectReferenceType.Target, x)));
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
    }

    private static async Task AddExecutionConditionParametersAsync(
        TStep step,
        CreateExecutionConditionParameter[] parameters,
        AppDbContext dbContext,
        CancellationToken cancellationToken)
    {
        // Fetch potential job parameter ids from DB and check matching ids.
        if (parameters.Any(x => x.InheritFromJobParameterId is not null))
        {
            var jobParameterIds = parameters
                .Select(x => x.InheritFromJobParameterId)
                .OfType<Guid>()
                .ToArray();
            var jobParameterIdsFromDb = await dbContext
                .Set<JobParameter>()
                .Where(x => jobParameterIds.Contains(x.ParameterId))
                .Select(x => x.ParameterId)
                .ToArrayAsync(cancellationToken);
            foreach (var id in jobParameterIds)
            {
                if (!jobParameterIdsFromDb.Contains(id))
                {
                    throw new NotFoundException<JobParameter>(id);
                }
            }
        }
        
        foreach (var parameter in parameters)
        {
            step.ExecutionConditionParameters.Add(new ExecutionConditionParameter
            {
                ParameterName = parameter.ParameterName,
                ParameterValue = parameter.ParameterValue,
                JobParameterId = parameter.InheritFromJobParameterId
            });
        }
    }
}