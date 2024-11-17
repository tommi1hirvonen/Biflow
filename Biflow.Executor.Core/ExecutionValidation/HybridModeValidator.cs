using Biflow.Executor.Core.Projections;
using Microsoft.Extensions.Logging;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;

namespace Biflow.Executor.Core.ExecutionValidation;

internal class HybridModeValidator(
    ILogger<HybridModeValidator> logger,
    IDbContextFactory<ExecutorDbContext> dbContextFactory) : IExecutionValidator
{
    private readonly ILogger<HybridModeValidator> _logger = logger;
    private readonly IDbContextFactory<ExecutorDbContext> _dbContextFactory = dbContextFactory;
    private readonly JsonSerializerOptions _serializerOptions = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.Create(UnicodeRanges.All)
    };

    public async Task<bool> ValidateAsync(Execution execution, Func<string, Task> onValidationFailed, CancellationToken cancellationToken)
    {
        if (execution.ExecutionMode != ExecutionMode.Hybrid)
        {
            return true;
        }

        string? illegalSteps;
        try
        {
            illegalSteps = await ReadIllegalHybridModeStepsAsync(execution.JobId, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{executionId} Error checking for illegal hybrid mode steps", execution.ExecutionId);
            await onValidationFailed("Error checking for possible illegal hybrid mode steps");
            return false;
        }

        if (string.IsNullOrEmpty(illegalSteps))
        {
            return true;
        }
        
        var errorMessage = "Detected steps causing infinite wait in hybrid mode:\n" + illegalSteps;
        await onValidationFailed(errorMessage);
        return false;
    }

    private async Task<string?> ReadIllegalHybridModeStepsAsync(Guid jobId, CancellationToken cancellationToken)
    {
        // Checks for steps that cause infinite waiting in hybrid execution mode.
        await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var steps = await context.Dependencies
            .AsNoTracking()
            .Where(d => d.Step.JobId == jobId)
            // Dependencies exist where the step's execution phase is lower than the dependent step's execution phase.
            .Where(d => d.Step.ExecutionPhase < d.DependantOnStep.ExecutionPhase && d.Step.JobId == d.DependantOnStep.JobId)
            .Select(d => new
            {
                Step = new StepProjection(d.Step.StepId, d.Step.StepName),
                DependantOnStep = new StepProjection(d.DependantOnStep.StepId, d.DependantOnStep.StepName)
            })
            .ToListAsync(cancellationToken);
        return steps.Count > 0
            ? JsonSerializer.Serialize(steps, _serializerOptions)
            : null;
    }
}
