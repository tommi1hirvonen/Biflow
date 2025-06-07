using Biflow.Executor.Core.Notification;
using Microsoft.Extensions.Logging;

namespace Biflow.Executor.Core.StepExecutor;

[UsedImplicitly]
internal class EmailStepExecutor(
    ILogger<EmailStepExecutor> logger,
    IDbContextFactory<ExecutorDbContext> dbContextFactory,
    IMessageDispatcher messageDispatcher)
    : StepExecutor<EmailStepExecution, EmailStepExecutionAttempt>(logger, dbContextFactory)
{
    private readonly IMessageDispatcher _messageDispatcher = messageDispatcher;

    protected override async Task<Result> ExecuteAsync(
        OrchestrationContext context,
        EmailStepExecution step,
        EmailStepExecutionAttempt attempt,
        ExtendedCancellationTokenSource cancellationTokenSource)
    {
        var cancellationToken = cancellationTokenSource.Token;
        cancellationToken.ThrowIfCancellationRequested();

        var parameters = step.StepExecutionParameters.ToStringDictionary();

        var recipients = step.GetRecipientsAsList().Select(r => r.Replace(parameters));
        var subject = step.Subject.Replace(parameters);
        var body = step.Body.Replace(parameters);

        await _messageDispatcher.SendMessageAsync(recipients, subject, body, false, cancellationToken);

        return Result.Success;
    }
}
