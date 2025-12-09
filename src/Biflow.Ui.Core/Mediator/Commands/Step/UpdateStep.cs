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
    public required IDictionary<Guid, DependencyType> Dependencies { get; init; }
    public required UpdateExecutionConditionParameter[] ExecutionConditionParameters { get; init; }
    public required DataObjectRelation[] Sources { get; init; }
    public required DataObjectRelation[] Targets { get; init; }
}

public abstract class UpdateStepCommandHandler<TCommand, TStep>(
    IDbContextFactory<AppDbContext> dbContextFactory,
    StepValidator validator
) : IRequestHandler<TCommand, TStep>
    where TCommand : UpdateStepCommand<TStep>
    where TStep : Step
{
    public async Task<TStep> Handle(TCommand request, CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        
        var step = await GetStepAsync(request.StepId, dbContext, cancellationToken)
            ?? throw new NotFoundException<TStep>(request.StepId);
        
        step.StepName = request.StepName;
        step.StepDescription = request.StepDescription;
        step.ExecutionPhase = request.ExecutionPhase;
        step.DuplicateExecutionBehaviour = request.DuplicateExecutionBehaviour;
        step.IsEnabled = request.IsEnabled;
        step.RetryAttempts = request.RetryAttempts;
        step.RetryIntervalMinutes = request.RetryIntervalMinutes;
        step.ExecutionConditionExpression.Expression = request.ExecutionConditionExpression;
        
        await UpdateTypeSpecificPropertiesAsync(step, request, dbContext, cancellationToken);
        await SynchronizeDependenciesAsync(step, request.Dependencies, dbContext, cancellationToken);
        await SynchronizeExecutionConditionParametersAsync(step, request.ExecutionConditionParameters, dbContext,
            cancellationToken);
        await SynchronizeTagsAsync(step, request.StepTagIds, dbContext, cancellationToken);
        await SynchronizeDataObjectsAsync(step, request.Sources, DataObjectReferenceType.Source, dbContext,
            cancellationToken);
        await SynchronizeDataObjectsAsync(step, request.Targets, DataObjectReferenceType.Target, dbContext,
            cancellationToken);
        
        step.EnsureDataAnnotationsValidated();
        await validator.EnsureValidatedAsync(step, cancellationToken);
        
        await dbContext.SaveChangesAsync(cancellationToken);

        return step;
    }
    
    protected abstract Task<TStep?> GetStepAsync(
        Guid stepId, AppDbContext dbContext, CancellationToken cancellationToken);
    
    protected abstract Task UpdateTypeSpecificPropertiesAsync(
        TStep step, TCommand request, AppDbContext dbContext, CancellationToken cancellationToken);

    protected async Task SynchronizeParametersAsync<TParameter, TUpdateParameter>(
        IHasStepParameters<TParameter> step,
        TUpdateParameter[] parameters,
        Func<TUpdateParameter, TParameter> parameterDelegate,
        AppDbContext dbContext,
        CancellationToken cancellationToken)
        where TParameter : StepParameterBase
        where TUpdateParameter : UpdateStepParameter
    {
        // Fetch potential job parameter ids from DB and check matching ids.
        var jobParameterIds = parameters
            .Select(x => x.InheritFromJobParameterId)
            .OfType<Guid>()
            .Concat(parameters.SelectMany(x => x.ExpressionParameters).Select(x => x.InheritFromJobParameterId))
            .Distinct()
            .ToArray();
        if (jobParameterIds.Length > 0)
        {
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
        
        // Remove parameters
        var parametersToRemove = step.StepParameters
            .Where(p1 => parameters.All(p2 => p2.ParameterId != p1.ParameterId))
            .ToArray();
        foreach (var parameter in parametersToRemove)
        {
            step.StepParameters.Remove(parameter);
        }
        
        // Update matching parameters
        foreach (var parameter in step.StepParameters)
        {
            var updateParameter = parameters
                .FirstOrDefault(p => p.ParameterId == parameter.ParameterId);
            if (updateParameter is null) continue;
            parameter.ParameterName = updateParameter.ParameterName;
            parameter.ParameterValue = updateParameter.ParameterValue;
            parameter.UseExpression = updateParameter.UseExpression;
            parameter.Expression.Expression = updateParameter.Expression;
            parameter.InheritFromJobParameterId = updateParameter.InheritFromJobParameterId;
            
            // Remove expression parameters
            var expressionParametersToRemove = parameter.ExpressionParameters
                .Where(p1 => updateParameter.ExpressionParameters.All(p2 => p2.ParameterId != p1.ParameterId))
                .ToArray();
            foreach (var expressionParameter in expressionParametersToRemove)
                parameter.RemoveExpressionParameter(expressionParameter);
            
            // Update matching expression parameters
            foreach (var expressionParameter in parameter.ExpressionParameters)
            {
                var updateExpressionParameter = updateParameter.ExpressionParameters
                    .FirstOrDefault(p => p.ParameterId == expressionParameter.ParameterId);
                if (updateExpressionParameter is null) continue;
                expressionParameter.ParameterName = updateExpressionParameter.ParameterName;
                expressionParameter.InheritFromJobParameterId = updateExpressionParameter.InheritFromJobParameterId;
            }
            
            // Add expression parameters
            var expressionParametersToAdd = updateParameter.ExpressionParameters
                .Where(p1 => parameter.ExpressionParameters.All(p2 => p2.ParameterId != p1.ParameterId))
                .ToArray();
            foreach (var createExpressionParameter in expressionParametersToAdd)
            {
                parameter.AddExpressionParameter(
                    createExpressionParameter.ParameterName,
                    createExpressionParameter.InheritFromJobParameterId);
            }
        }
        
        // Add parameters
        var parametersToAdd = parameters
            .Where(p1 => step.StepParameters.All(p2 => p2.ParameterId != p1.ParameterId))
            .ToArray(); // Materialize the results. When multiple new parameters are added
                        // the id comparison will not work correctly,
                        // because multiple parameters will have an id value of Guid.Empty.
        foreach (var createParameter in parametersToAdd)
        {
            var parameter = parameterDelegate(createParameter);
            step.StepParameters.Add(parameter);
            foreach (var expressionParameter in createParameter.ExpressionParameters)
            {
                parameter.AddExpressionParameter(
                    expressionParameter.ParameterName,
                    expressionParameter.InheritFromJobParameterId);
            }
        }
    }

    private static async Task SynchronizeDependenciesAsync(
        Step step,
        IDictionary<Guid, DependencyType> dependencies,
        AppDbContext dbContext,
        CancellationToken cancellationToken)
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

        foreach (var dependency in step.Dependencies)
        {
            if (dependencies.TryGetValue(dependency.DependantOnStepId, out var dependencyType))
            {
                dependency.DependencyType = dependencyType;
            }
        }

        var dependenciesToAdd = dependencies
            .Where(x => step.Dependencies.All(d => x.Key != d.DependantOnStepId))
            .Select(x => new Dependency
            {
                StepId = step.StepId,
                DependantOnStepId = x.Key,
                DependencyType = x.Value
            });
        
        foreach (var dependency in dependenciesToAdd) step.Dependencies.Add(dependency);
        
        var dependenciesToRemove = step.Dependencies
            .Where(d => dependencies.All(x => x.Key != d.DependantOnStepId))
            .ToArray();
        
        foreach (var dependency in dependenciesToRemove) step.Dependencies.Remove(dependency);
    }

    private static async Task SynchronizeExecutionConditionParametersAsync(
        Step step,
        UpdateExecutionConditionParameter[] parameters,
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
        
        // Remove parameters
        var parametersToRemove = step.ExecutionConditionParameters
            .Where(p1 => parameters.All(p2 => p2.ParameterId != p1.ParameterId))
            .ToArray();
        foreach (var parameter in parametersToRemove)
        {
            step.ExecutionConditionParameters.Remove(parameter);
        }
        
        // Update matching parameters
        foreach (var parameter in step.ExecutionConditionParameters)
        {
            var updateParameter = parameters
                .FirstOrDefault(p => p.ParameterId == parameter.ParameterId);
            if (updateParameter is null) continue;
            parameter.ParameterName = updateParameter.ParameterName;
            parameter.ParameterValue = updateParameter.ParameterValue;
            parameter.JobParameterId = updateParameter.InheritFromJobParameterId;
        }

        // Add parameters
        var parametersToAdd = parameters
            .Where(p1 => step.ExecutionConditionParameters.All(p2 => p2.ParameterId != p1.ParameterId))
            .ToArray();
        foreach (var createParameter in parametersToAdd)
        {
            step.ExecutionConditionParameters.Add(new ExecutionConditionParameter
            {
                ParameterName = createParameter.ParameterName,
                ParameterValue = createParameter.ParameterValue,
                JobParameterId = createParameter.InheritFromJobParameterId
            });
        }
    }

    private static async Task SynchronizeDataObjectsAsync(Step step, DataObjectRelation[] relations,
        DataObjectReferenceType referenceType, AppDbContext dbContext, CancellationToken cancellationToken)
    {
        var dataObjectIds = relations
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
        
        var relationsToRemove = step.DataObjects
            .Where(x => x.ReferenceType == referenceType && relations.All(y => y.DataObjectId != x.ObjectId))
            .ToArray();
        foreach (var relation in relationsToRemove) step.DataObjects.Remove(relation);

        foreach (var relation in step.DataObjects.Where(x => x.ReferenceType == referenceType))
        {
            var updateRelation = relations
                .FirstOrDefault(x => x.DataObjectId == relation.ObjectId);
            if (updateRelation is null || relation.DataAttributes.SequenceEqual(updateRelation.DataAttributes))
                continue;
            relation.DataAttributes.Clear();
            relation.DataAttributes.AddRange(updateRelation.DataAttributes.Distinct().Order());
        }

        var relationsToAdd = relations
            .Where(x => step.DataObjects.Where(y => y.ReferenceType == referenceType)
                .All(y => y.ObjectId != x.DataObjectId));
        foreach (var (dataObjectId, dataAttributes) in relationsToAdd)
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

    private static async Task SynchronizeTagsAsync(
        Step step,
        Guid[] stepTagIds,
        AppDbContext dbContext,
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
        
        // Synchronize tags
        var stepTagsToAdd = stepTags.Where(t1 => step.Tags.All(t2 => t2.TagId != t1.TagId));
        foreach (var tag in stepTagsToAdd)
        {
            step.Tags.Add(tag);
        }
        var stepTagsToRemove = step.Tags
            .Where(t => !stepTagIds.Contains(t.TagId))
            .ToArray(); // Materialize results because items may be removed from the sequence during iteration.
        foreach (var tag in stepTagsToRemove)
        {
            step.Tags.Remove(tag);
        }
    }
}