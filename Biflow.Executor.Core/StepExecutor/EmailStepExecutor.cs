using Biflow.DataAccess.Models;
using Biflow.Executor.Core.Common;
using Biflow.Executor.Core.Notification;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Biflow.Executor.Core.StepExecutor;

internal class EmailStepExecutor(
    ILogger<EmailStepExecutor> logger,
    IDbContextFactory<ExecutorDbContext> dbContextFactory,
    IMessageDispatcher messageDispatcher,
    EmailStepExecution stepExecution) : StepExecutorBase(logger, dbContextFactory, stepExecution)
{
    private readonly IMessageDispatcher _messageDispatcher = messageDispatcher;
    private readonly EmailStepExecution _step = stepExecution;

    protected override async Task<Result> ExecuteAsync(ExtendedCancellationTokenSource cancellationTokenSource)
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
