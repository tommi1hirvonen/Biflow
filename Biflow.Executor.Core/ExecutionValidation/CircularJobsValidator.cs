using Biflow.Executor.Core.Projections;
using Microsoft.Extensions.Logging;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;

namespace Biflow.Executor.Core.ExecutionValidation;

internal class CircularJobsValidator(
    ILogger<CircularJobsValidator> logger,
    IDbContextFactory<ExecutorDbContext> dbContextFactory) : IExecutionValidator
{
    private readonly ILogger<CircularJobsValidator> _logger = logger;
    private readonly IDbContextFactory<ExecutorDbContext> _dbContextFactory = dbContextFactory;
    private readonly JsonSerializerOptions _serializerOptions = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.Create(UnicodeRanges.All)
    };

    public async Task<bool> ValidateAsync(Execution execution, Func<string, Task> onValidationFailed, CancellationToken cancellationToken)
    {
        // Check whether there are circular dependencies between jobs (through steps executing another jobs).
        string? circularExecutions;
        try
        {
            circularExecutions = await GetCircularJobExecutionsAsync(execution.JobId, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{executionId} Error checking for possible circular job executions", execution.ExecutionId);
            await onValidationFailed("Error checking for possible circular job executions");
            return false;
        }

        if (!string.IsNullOrEmpty(circularExecutions))
        {
            var errorMessage = "Circular job executions detected:\n" + circularExecutions;
            await onValidationFailed(errorMessage);
            return false;
        }

        return true;
    }

    /// <summary>
    /// Checks for circular dependencies between jobs.
    /// Jobs can reference other jobs, so it's important to check them for circlular dependencies.
    /// </summary>
    /// <param name="executionConfig"></param>
    /// <returns>
    /// JSON string of circular job dependencies or null if there were no circular dependencies.
    /// </returns>
    private async Task<string?> GetCircularJobExecutionsAsync(Guid jobId, CancellationToken cancellationToken)
    {
        var dependencies = await ReadJobDependenciesAsync(cancellationToken);
        IEnumerable<IEnumerable<JobProjection>> cycles = dependencies.FindCycles();
        var json = JsonSerializer.Serialize(cycles, _serializerOptions);

        // There are no circular dependencies or this job is not among the cycles.
        return !cycles.Any() || !cycles.Any(jobs => jobs.Any(j => j.JobId == jobId))
            ? null : json;
    }

    private async Task<Dictionary<JobProjection, JobProjection[]>> ReadJobDependenciesAsync(CancellationToken cancellationToken)
    {
        using var context = _dbContextFactory.CreateDbContext();
        var jobs = await context.JobSteps
            .AsNoTracking()
            .Select(step => new
            {
                Job = new JobProjection(step.Job.JobId, step.Job.JobName),
                JobToExecute = new JobProjection(step.JobToExecute.JobId, step.JobToExecute.JobName)
            })
            .ToArrayAsync(cancellationToken);
        var dependencies = jobs
            .GroupBy(key => key.Job, element => element.JobToExecute)
            .ToDictionary(grouping => grouping.Key, grouping => grouping.ToArray());
        return dependencies;
    }
}
