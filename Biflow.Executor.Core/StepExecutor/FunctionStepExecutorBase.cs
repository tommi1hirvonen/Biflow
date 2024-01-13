using Biflow.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Biflow.Executor.Core.StepExecutor;

internal abstract class FunctionStepExecutorBase(
    ILogger<FunctionStepExecutorBase> logger,
    IDbContextFactory<ExecutorDbContext> dbContextFactory,
    FunctionStepExecution step)
{
    private readonly ILogger<FunctionStepExecutorBase> _logger = logger;
    private readonly IDbContextFactory<ExecutorDbContext> _dbContextFactory = dbContextFactory;

    protected FunctionStepExecution Step { get; } = step;

    protected FunctionApp FunctionApp { get; } = step.GetApp()
        ?? throw new ArgumentNullException(nameof(FunctionApp));

    protected async Task<HttpRequestMessage> BuildFunctionInvokeRequestAsync(FunctionStepExecutionAttempt attempt, CancellationToken cancellationToken)
    {
        string? functionKey = null;
        try
        {
            // Try and get the function key from the actual step if it was defined.
            using var context = _dbContextFactory.CreateDbContext();
            functionKey = await context.FunctionSteps
                .AsNoTracking()
                .Where(step => step.StepId == Step.StepId)
                .Select(step => step.FunctionKey)
                .FirstOrDefaultAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{ExecutionId} {Step} Error reading FunctionKey from database", Step.ExecutionId, Step);
            attempt.AddWarning(ex, "Error reading function key from database");
        }

        var message = new HttpRequestMessage(HttpMethod.Post, Step.FunctionUrl);

        // Add function security code as a request header. If the function specific code was defined, use that.
        // Otherwise revert to the function app code if it was defined.
        functionKey ??= FunctionApp.FunctionAppKey;
        if (!string.IsNullOrEmpty(functionKey))
        {
            message.Headers.Add("x-functions-key", functionKey);
        }

        // If the input for the function was defined, add it to the request content.
        if (!string.IsNullOrEmpty(Step.FunctionInput))
        {
            var parameters = Step.StepExecutionParameters.ToStringDictionary();
            var input = Step.FunctionInput.Replace(parameters);
            message.Content = new StringContent(input);
        }

        return message;
    }
}
