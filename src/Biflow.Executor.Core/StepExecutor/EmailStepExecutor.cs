using Biflow.Executor.Core.Notification;
using Microsoft.Extensions.Logging;

namespace Biflow.Executor.Core.StepExecutor;

[UsedImplicitly]
internal class EmailStepExecutor(
    ILogger<EmailStepExecutor> logger,
    EmailStepExecution step,
    EmailStepExecutionAttempt attempt,
    IMessageDispatcher? messageDispatcher = null) : IStepExecutor
{
    public async Task<Result> ExecuteAsync(OrchestrationContext context, ExtendedCancellationTokenSource cts)
    {
        var cancellationToken = cts.Token;
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
