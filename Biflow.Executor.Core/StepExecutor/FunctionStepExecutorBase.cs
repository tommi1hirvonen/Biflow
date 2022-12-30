using Biflow.DataAccess;
using Biflow.DataAccess.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Biflow.Executor.Core.StepExecutor;

internal abstract class FunctionStepExecutorBase : StepExecutorBase
{
    private readonly ILogger<FunctionStepExecutorBase> _logger;
    private readonly IDbContextFactory<BiflowContext> _dbContextFactory;

    protected FunctionStepExecution Step { get; }

    public FunctionStepExecutorBase(
        ILogger<FunctionStepExecutorBase> logger,
        IDbContextFactory<BiflowContext> dbContextFactory,
        FunctionStepExecution step)
        : base(logger, dbContextFactory, step)
    {
        _logger = logger;
        _dbContextFactory = dbContextFactory;
        Step = step;
    }

    protected async Task<HttpRequestMessage> BuildFunctionInvokeRequestAsync(CancellationToken cancellationToken)
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
            AddWarning(ex, "Error reading function key from database");
        }

        var message = new HttpRequestMessage(HttpMethod.Post, Step.FunctionUrl);

        // Add function security code as a request header. If the function specific code was defined, use that.
        // Otherwise revert to the function app code if it was defined.
        functionKey ??= Step.FunctionApp.FunctionAppKey;
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
