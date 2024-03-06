using Biflow.Executor.Core.Projections;
using Microsoft.Extensions.Logging;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;

namespace Biflow.Executor.Core.ExecutionValidation;

internal class CircularStepsValidator(
    ILogger<CircularStepsValidator> logger,
    IDbContextFactory<ExecutorDbContext> dbContextFactory) : IExecutionValidator
{
    private readonly ILogger<CircularStepsValidator> _logger = logger;
    private readonly IDbContextFactory<ExecutorDbContext> _dbContextFactory = dbContextFactory;
    private readonly JsonSerializerOptions _serializerOptions = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.Create(UnicodeRanges.All)
    };

    public async Task<bool> ValidateAsync(Execution execution, Func<string, Task> onValidationFailed, CancellationToken cancellationToken)
    {
        string? circularSteps;
        try
        {
            circularSteps = await GetCircularStepDependenciesAsync(execution.JobId, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{executionId} Error checking for possible circular step dependencies", execution.ExecutionId);
            await onValidationFailed("Error checking for possible circular step dependencies");
            return false;
        }

        if (!string.IsNullOrEmpty(circularSteps))
        {
            var errorMessage = "Circular step dependencies detected:\n" + circularSteps;
            await onValidationFailed(errorMessage);
            return false;
        }

        return true;
    }

    private async Task<string?> GetCircularStepDependenciesAsync(Guid jobId, CancellationToken cancellationToken)
    {
        // Find circular step dependencies which are not allowed since they would block each other's executions.
        var dependencies = await ReadStepDependenciesAsync(jobId, cancellationToken);
        IEnumerable<IEnumerable<StepProjection>> cycles = dependencies.FindCycles();
        var json = JsonSerializer.Serialize(cycles, _serializerOptions);
        return !cycles.Any() ? null : json;
    }

    private async Task<Dictionary<StepProjection, StepProjection[]>> ReadStepDependenciesAsync(Guid jobId, CancellationToken cancellationToken)
    {
        using var context = _dbContextFactory.CreateDbContext();
        var steps = await context.Dependencies
            .AsNoTracking()
            .Where(d => d.Step.JobId == jobId)
            .Select(d => new
            {
                Step = new StepProjection(d.Step.StepId, d.Step.StepName),
                DependantOnStep = new StepProjection(d.DependantOnStep.StepId, d.DependantOnStep.StepName)
            })
            .ToArrayAsync(cancellationToken);
        var dependencies = steps
            .GroupBy(key => key.Step, element => element.DependantOnStep)
            .ToDictionary(g => g.Key, g => g.ToArray());
        return dependencies;
    }
}
