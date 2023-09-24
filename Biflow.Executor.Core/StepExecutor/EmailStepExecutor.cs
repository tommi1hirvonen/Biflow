using Biflow.DataAccess.Models;
using Biflow.Executor.Core.Common;
using Biflow.Executor.Core.Notification;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Biflow.Executor.Core.StepExecutor;

internal class EmailStepExecutor : StepExecutorBase
{
    private readonly IMessageDispatcher _messageDispatcher;
    
    private EmailStepExecution Step { get; }

    public EmailStepExecutor(
        ILogger<EmailStepExecutor> logger,
        IDbContextFactory<ExecutorDbContext> dbContextFactory,
        IMessageDispatcher messageDispatcher,
        EmailStepExecution stepExecution)
        : base(logger, dbContextFactory, stepExecution)
    {
        _messageDispatcher = messageDispatcher;
        Step = stepExecution;
    }

    protected override async Task<Result> ExecuteAsync(ExtendedCancellationTokenSource cancellationTokenSource)
    {
        var cancellationToken = cancellationTokenSource.Token;
        cancellationToken.ThrowIfCancellationRequested();

        var parameters = Step.StepExecutionParameters.ToStringDictionary();

        var recipients = Step.GetRecipientsAsList().Select(r => r.Replace(parameters));
        var subject = Step.Subject.Replace(parameters);
        var body = Step.Body.Replace(parameters);

        await _messageDispatcher.SendMessageAsync(recipients, subject, body, false, cancellationToken);

        return new Success();
    }
}
