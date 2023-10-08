using Biflow.DataAccess.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace Biflow.DataAccess;

internal class ExecutionBuilderFactory<TDbContext>(IDbContextFactory<TDbContext> dbContextFactory) : IExecutionBuilderFactory<TDbContext> where TDbContext : AppDbContext
{
    private readonly IDbContextFactory<TDbContext> _dbContextFactory = dbContextFactory;

    public Task<ExecutionBuilder?> CreateAsync(Guid jobId, string? createdBy, params Func<TDbContext, Expression<Func<Step, bool>>>[] predicates) =>
        CreateAsync(jobId, createdBy, null, predicates);

    public async Task<ExecutionBuilder?> CreateAsync(Guid jobId, string? createdBy, StepExecutionAttempt? parent, params Func<TDbContext, Expression<Func<Step, bool>>>[] predicates)
    {
        var data = await GetBuilderDataAsync(jobId, predicates);
        if (data is null)
        {
            return null;
        }
        var (context, job, steps) = data;
        Execution createExecution() => new(job, createdBy, parent);
        return new ExecutionBuilder(context, createExecution, steps);
    }

    public async Task<ExecutionBuilder?> CreateAsync(Guid jobId, Guid scheduleId, params Func<TDbContext, Expression<Func<Step, bool>>>[] predicates)
    {
        var data = await GetBuilderDataAsync(jobId, predicates);
        if (data is null)
        {
            return null;
        }
        var (context, job, steps) = data;
        var schedule = await context.Schedules
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.ScheduleId == scheduleId);
        Execution createExecution() => new(job, schedule);
        return new ExecutionBuilder(context, createExecution, steps);
    }

    private async Task<BuilderData?> GetBuilderDataAsync(Guid jobId, Func<TDbContext, Expression<Func<Step, bool>>>[] predicates)
    {
        var context = await _dbContextFactory.CreateDbContextAsync();
        var job = await context.Jobs
            .AsNoTrackingWithIdentityResolution()
            .Include(j => j.JobParameters)
            .Include(j => j.JobConcurrencies)
            .FirstOrDefaultAsync(j => j.JobId == jobId);
        if (job is null)
        {
            return null;
        }
        var stepsQuery = context.Steps
            .AsNoTrackingWithIdentityResolution()
            .Where(s => s.JobId == job.JobId);
        foreach (var predicate in predicates)
        {
            stepsQuery = stepsQuery.Where(predicate(context));
        }
        var steps = await stepsQuery
            .Include($"{nameof(IHasStepParameters.StepParameters)}.{nameof(StepParameterBase.ExpressionParameters)}")
            .Include($"{nameof(IHasStepParameters.StepParameters)}.{nameof(StepParameterBase.InheritFromJobParameter)}")
            .Include(s => (s as JobStep)!.TagFilters)
            .Include(s => s.Dependencies)
            .Include(s => s.Targets)
            .Include(s => s.Sources)
            .Include(s => s.Tags)
            .Include(s => s.ExecutionConditionParameters)
            .ToArrayAsync();
        return new(context, job, steps);
    }

    private record BuilderData(TDbContext Context, Job Job, Step[] Steps);
}