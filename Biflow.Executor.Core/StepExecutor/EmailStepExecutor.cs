using Biflow.DataAccess;
using Biflow.DataAccess.Models;
using Biflow.Executor.Core.Common;
using Biflow.Executor.Core.Notification;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Biflow.Executor.Core.StepExecutor;

internal class EmailStepExecutor : StepExecutorBase
{
    private readonly INotificationService _notificationService;
    private readonly EmailStepExecution _stepExecution;

    public EmailStepExecutor(
        ILogger<EmailStepExecutor> logger,
        IDbContextFactory<BiflowContext> dbContextFactory,
        INotificationService notificationService,
        EmailStepExecution stepExecution)
        : base(logger, dbContextFactory, stepExecution)
    {
        _notificationService = notificationService;
        _stepExecution = stepExecution;
    }

    protected override async Task<Result> ExecuteAsync(ExtendedCancellationTokenSource cancellationTokenSource)
    {
        var cancellationToken = cancellationTokenSource.Token;
        cancellationToken.ThrowIfCancellationRequested();

        var recipients = _stepExecution.GetRecipientsAsList();

        var subject = _stepExecution.Subject;
        var body = _stepExecution.Body;

        // Iterate parameters and replace parameter names with corresponding values.
        // Do this for both the subject and body.
        foreach (var param in _stepExecution.StepExecutionParameters)
        {
            var value = param.ParameterValue switch
            {
                DateTime dt => dt.ToString("o"),
                _ => param.ParameterValue.ToString()
            };
            recipients = recipients.Select(r => r.Replace(param.ParameterName, value)).ToList();
            subject = subject.Replace(param.ParameterName, value);
            body = body.Replace(param.ParameterName, value);
        }

        await _notificationService.SendNotification(recipients, subject, body, cancellationToken);

        return Result.Success();
    }
}
