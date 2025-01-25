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
    protected abstract Task<TStep?> GetStepAsync(Guid stepId, AppDbContext dbContext, CancellationToken cancellationToken);
    
    protected abstract Task UpdatePropertiesAsync(
        TStep step, TCommand request, AppDbContext dbContext, CancellationToken cancellationToken);

    protected void SynchronizeParameters<TParameter, TUpdateParameter>(
        IHasStepParameters<TParameter> step,
        TUpdateParameter[] parameters,
        Func<TUpdateParameter, TParameter> parameterDelegate)
        where TParameter : StepParameterBase
        where TUpdateParameter : UpdateStepParameter
    {
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
                .Where(p1 => parameter.ExpressionParameters.All(p2 => p2.ParameterId != p1.ParameterId));
            foreach (var createExpressionParameter in expressionParametersToAdd)
            {
                parameter.AddExpressionParameter(
                    createExpressionParameter.ParameterName,
                    createExpressionParameter.InheritFromJobParameterId);
            }
        }
        
        // Add parameters
        var parametersToAdd = parameters
            .Where(p1 => step.StepParameters.All(p2 => p2.ParameterId != p1.ParameterId));
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
    
    public async Task<TStep> Handle(TCommand request, CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        
        var step = await GetStepAsync(request.StepId, dbContext, cancellationToken)
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