using Biflow.Executor.Core.Notification;
using Microsoft.Extensions.Logging;

namespace Biflow.Executor.Core.StepExecutor;

[UsedImplicitly]
internal class EmailStepExecutor(
    ILogger<EmailStepExecutor> logger,
    IDbContextFactory<ExecutorDbContext> dbContextFactory,
    IMessageDispatcher? messageDispatcher = null)
    : StepExecutor<EmailStepExecution, EmailStepExecutionAttempt>(logger, dbContextFactory)
{
    protected override async Task<Result> ExecuteAsync(
        OrchestrationContext context,
        EmailStepExecution step,
        EmailStepExecutionAttempt attempt,
        ExtendedCancellationTokenSource cancellationTokenSource)
    {
        var cancellationToken = cancellationTokenSource.Token;
        cancellationToken.ThrowIfCancellationRequested();

        if (messageDispatcher is null)
        {
            logger.LogError("Email step execution failed because no message dispatcher was configured.");
            attempt.AddError("No message dispatcher was configured in the application settings.");
            return Result.Failure;
        }

        var parameters = step.StepExecutionParameters.ToStringDictionary();

        var recipients = step.GetRecipientsAsList().Select(r => r.Replace(parameters));
        var subject = step.Subject.Replace(parameters);
        var body = step.Body.Replace(parameters);

        await messageDispatcher.SendMessageAsync(recipients, subject, body, false, cancellationToken);

        return Result.Success;
    }
}
