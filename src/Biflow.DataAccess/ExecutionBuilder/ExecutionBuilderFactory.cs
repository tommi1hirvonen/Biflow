using System.Linq.Expressions;

namespace Biflow.DataAccess;

internal class ExecutionBuilderFactory<TDbContext>(IDbContextFactory<TDbContext> dbContextFactory)
    : IExecutionBuilderFactory<TDbContext> where TDbContext : AppDbContext
{
    private readonly IDbContextFactory<TDbContext> _dbContextFactory = dbContextFactory;

    public Task<ExecutionBuilder?> CreateAsync(
        Guid jobId,
        string? createdBy,
        IEnumerable<Func<TDbContext, Expression<Func<Step, bool>>>>? predicates = null,
        CancellationToken cancellationToken = default) => CreateAsync(jobId, createdBy, null, predicates, cancellationToken);

    public async Task<ExecutionBuilder?> CreateAsync(
        Guid jobId,
        string? createdBy,
        StepExecutionAttempt? parent,
        IEnumerable<Func<TDbContext, Expression<Func<Step, bool>>>>? predicates = null,
        CancellationToken cancellationToken = default)
    {
        var data = await GetBuilderDataAsync(jobId, predicates, cancellationToken);
        if (data is null)
        {
            return null;
        }
        var (context, job, steps) = data;
        return new ExecutionBuilder(context, CreateExecution, steps);
        Execution CreateExecution() => new(job, createdBy, parent);
    }

    public async Task<ExecutionBuilder?> CreateAsync(
        Guid jobId,
        Guid scheduleId,
        IEnumerable<Func<TDbContext, Expression<Func<Step, bool>>>>? predicates = null,
        CancellationToken cancellationToken = default)
    {
        var data = await GetBuilderDataAsync(jobId, predicates, cancellationToken);
        if (data is null)
        {
            return null;
        }
        var (context, job, steps) = data;
        var schedule = await context.Schedules
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.ScheduleId == scheduleId, cancellationToken);
        return new ExecutionBuilder(context, CreateExecution, steps);
        Execution CreateExecution() => new(job, schedule);
    }

    private async Task<BuilderData?> GetBuilderDataAsync(
        Guid jobId,
        IEnumerable<Func<TDbContext, Expression<Func<Step, bool>>>>? predicates,
        CancellationToken cancellationToken)
    {
        var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var job = await context.Jobs
            .AsNoTrackingWithIdentityResolution()
            .Include(j => j.JobParameters)
            .Include(j => j.JobConcurrencies)
            .FirstOrDefaultAsync(j => j.JobId == jobId, cancellationToken);
        if (job is null)
        {
            return null;
        }
        var stepsQuery = context.Steps
            .AsNoTrackingWithIdentityResolution()
            .Where(s => s.JobId == job.JobId);
        stepsQuery = (predicates ?? [])
            .Aggregate(stepsQuery, (current, predicate) => current.Where(predicate(context)));
        var steps = await stepsQuery
            .Include($"{nameof(IHasStepParameters.StepParameters)}.{nameof(StepParameterBase.ExpressionParameters)}")
            .Include($"{nameof(IHasStepParameters.StepParameters)}.{nameof(StepParameterBase.InheritFromJobParameter)}")
            .Include(s => (s as JobStep)!.TagFilters)
            .Include(s => (s as ExeStep)!.RunAsCredential)
            .Include(s => s.Dependencies)
            .Include(s => s.DataObjects)
            .ThenInclude(t => t.DataObject)
            .Include(s => s.Tags)
            .Include(s => s.ExecutionConditionParameters)
            .ToArrayAsync(cancellationToken);
        return new(context, job, steps);
    }

    private record BuilderData(TDbContext Context, Job Job, Step[] Steps);
}