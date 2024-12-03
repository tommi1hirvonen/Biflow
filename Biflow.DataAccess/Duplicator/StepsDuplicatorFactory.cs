namespace Biflow.DataAccess;

public class StepsDuplicatorFactory(IDbContextFactory<AppDbContext> dbContextFactory)
{
    private readonly IDbContextFactory<AppDbContext> _dbContextFactory = dbContextFactory;

    /// <summary>
    /// Asynchronously creates a <see cref="StepsDuplicator"/> to copy selected steps
    /// </summary>
    /// <param name="stepId">id of the step that should be copied</param>
    /// <param name="targetJobId">id of the target job of the copy operation, or null
    /// if the target job is the same as the source</param>
    /// <returns><see cref="StepsDuplicator"/> which includes a copy of the step provided as argument</returns>
    public Task<StepsDuplicator> CreateAsync(Guid stepId, Guid? targetJobId) =>
        CreateAsync([stepId], targetJobId);
    
    /// <summary>
    /// Asynchronously creates a <see cref="StepsDuplicator"/> to copy selected steps
    /// </summary>
    /// <param name="stepIds">ids of the steps that should be copied</param>
    /// <param name="targetJobId">id of the target job of the copy operation, or null
    /// if the target job is the same as the source</param>
    /// <returns><see cref="StepsDuplicator"/> which includes copies of the steps provided as argument</returns>
    public async Task<StepsDuplicator> CreateAsync(Guid[] stepIds, Guid? targetJobId)
    {
        var context = await _dbContextFactory.CreateDbContextAsync();
        var query = context.Steps.Where(s => stepIds.Contains(s.StepId));
        query = DuplicatorExtensions.StepNavigationPaths
            .Aggregate(query, (current, include) => current.Include(include));
        var steps = await query.ToArrayAsync();
        var targetJob = targetJobId is { } id
            ? await context.Jobs.Include(j => j.JobParameters).FirstOrDefaultAsync(j => j.JobId == id)
            : null;
        
        // While creating copies of steps,
        // also create a mapping dictionary to map dependencies based on old step ids.
        var mapping = steps
            .Select(s => (Original: s, Copy: s.Copy(targetJob)))
            .ToDictionary(x => x.Original.StepId, x => x);
        var copies = mapping.Values
            .Select(map =>
            {
                // Map dependencies from ids to new ids.
                var dependencies = map.Original.Dependencies
                    .Select(d => MapDependency(map.Original, map.Copy, d))
                    .OfType<Dependency>();
                map.Copy.Dependencies.AddRange(dependencies);
                return map.Copy;
            })
            .ToList();
        
        return new StepsDuplicator(context, copies);
        
        Dependency? MapDependency(Step original, Step copy, Dependency dep)
        {
            // Map the dependent step's id from an old value to a new value using the dictionary.
            // In case no matching key is found, the dependency is not included in the steps that are copied.
            if (mapping.TryGetValue(dep.DependantOnStepId, out var map))
            {
                return new Dependency
                {
                    StepId = copy.StepId,
                    DependantOnStepId = map.Copy.StepId,
                    DependencyType = dep.DependencyType
                };
            }
            
            // If the dependency is not included in the steps that are copied,
            // check if the source and target jobs are the same => use the dependency as is.
            if (targetJobId is null || targetJobId == original.JobId)
            {
                return new Dependency
                {
                    StepId = copy.StepId,
                    DependantOnStepId = dep.DependantOnStepId,
                    DependencyType = dep.DependencyType
                };
            }
                
            return null;
        }
    }
}
