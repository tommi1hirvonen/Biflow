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

    public async Task<ExecutionBuilder?> CreateAsync(Guid jobId, string createdBy, Guid[]? stepIdFilter = null)
    {
        var data = await GetBuilderDataAsync(jobId, stepIdFilter);
        if (data is null)
        {
            return null;
        }
        var (context, job, steps) = data;
        var createExecution = () => new Execution(job, createdBy);
        return new ExecutionBuilder(context, createExecution, steps);
    }

    public async Task<ExecutionBuilder?> CreateAsync(Guid jobId, Guid scheduleId)
    {
        var data = await GetBuilderDataAsync(jobId, null);
        if (data is null)
        {
            return null;
        }
        var (context, job, steps) = data;
        var schedule = await context.Schedules
            .AsNoTracking()
            .FirstAsync(s => s.ScheduleId == scheduleId);
        var createExecution = () => new Execution(job, schedule);
        return new ExecutionBuilder(context, createExecution, steps);
    }

    private async Task<BuilderData?> GetBuilderDataAsync(Guid jobId, Guid[]? stepIdFilter)
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
        var steps = await context.Steps
            .AsNoTrackingWithIdentityResolution()
            .Where(s => s.JobId == job.JobId)
            .Where(s => stepIdFilter == null || stepIdFilter.Contains(s.StepId))
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

    private record BuilderData(BiflowContext Context, Job Job, Step[] Steps);
}