using Biflow.Executor.Core.Common;
using Biflow.Executor.Core.Notification;

namespace Biflow.Executor.Core.StepExecutor;

internal class EmailStepExecutor(
    IMessageDispatcher messageDispatcher,
    EmailStepExecution stepExecution) : IStepExecutor<EmailStepExecutionAttempt>
{
    private readonly IMessageDispatcher _messageDispatcher = messageDispatcher;
    private readonly EmailStepExecution _step = stepExecution;

    public EmailStepExecutionAttempt Clone(EmailStepExecutionAttempt other, int retryAttemptIndex) =>
        new(other, retryAttemptIndex);

    public async Task<Result> ExecuteAsync(EmailStepExecutionAttempt attempt, ExtendedCancellationTokenSource cancellationTokenSource)
    {
        var cancellationToken = cancellationTokenSource.Token;
        cancellationToken.ThrowIfCancellationRequested();

        var parameters = _step.StepExecutionParameters.ToStringDictionary();

        var recipients = _step.GetRecipientsAsList().Select(r => r.Replace(parameters));
        var subject = _step.Subject.Replace(parameters);
        var body = _step.Body.Replace(parameters);

        await _messageDispatcher.SendMessageAsync(recipients, subject, body, false, cancellationToken);

        return Result.Success;
    }
}
