using Biflow.DataAccess.Models;
using Microsoft.EntityFrameworkCore;

namespace Biflow.DataAccess;

internal class ExecutionBuilderFactory : IExecutionBuilderFactory
{
    private readonly IDbContextFactory<BiflowContext> _dbContextFactory;

    public ExecutionBuilderFactory(IDbContextFactory<BiflowContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    public async Task<ExecutionBuilder> CreateAsync(Guid jobId, string createdBy)
    {
        var (context, job, steps) = await GetContextAndDataAsync(jobId);
        var execution = new Execution(job, createdBy);
        return new ExecutionBuilder(context, execution, steps);
    }

    public async Task<ExecutionBuilder> CreateAsync(Guid jobId, Guid scheduleId)
    {
        var (context, job, steps) = await GetContextAndDataAsync(jobId);
        var schedule = await context.Schedules
            .AsNoTracking()
            .FirstAsync(s => s.ScheduleId == scheduleId);
        var execution = new Execution(job, schedule);
        return new ExecutionBuilder(context, execution, steps);
    }

    private async Task<(BiflowContext, Job, Step[])> GetContextAndDataAsync(Guid jobId)
    {
        var context = await _dbContextFactory.CreateDbContextAsync();
        var job = await context.Jobs
            .AsNoTrackingWithIdentityResolution()
            .Include(j => j.JobParameters)
            .Include(j => j.JobConcurrencies)
            .FirstAsync(j => j.JobId == jobId);
        var steps = await context.Steps
            .AsNoTrackingWithIdentityResolution()
            .Where(s => s.JobId == job.JobId)
            .Include($"{nameof(IHasStepParameters.StepParameters)}.{nameof(StepParameterBase.ExpressionParameters)}")
            .Include($"{nameof(IHasStepParameters.StepParameters)}.{nameof(StepParameterBase.InheritFromJobParameter)}")
            .Include(s => (s as JobStep)!.TagFilters)
            .Include(s => s.Dependencies)
            .Include(s => s.Targets)
            .Include(s => s.Sources)
            .Include(s => s.ExecutionConditionParameters)
            .ToArrayAsync();
        return (context, job, steps);
    }
}
