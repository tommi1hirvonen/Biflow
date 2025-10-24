using Biflow.Executor.Core.Notification;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Biflow.Executor.Core.StepExecutor;

internal class EmailStepExecutor(
    IServiceProvider serviceProvider,
    EmailStepExecution step,
    EmailStepExecutionAttempt attempt) : IStepExecutor
{
    private readonly ILogger<EmailStepExecutor> _logger = serviceProvider
        .GetRequiredService<ILogger<EmailStepExecutor>>();
    private readonly IMessageDispatcher? _messageDispatcher = serviceProvider.GetService<IMessageDispatcher>();
    
    public async Task<Result> ExecuteAsync(OrchestrationContext context, ExtendedCancellationTokenSource cts)
    {
        var cancellationToken = cts.Token;
        cancellationToken.ThrowIfCancellationRequested();

        if (_messageDispatcher is null)
        {
            _logger.LogError("Email step execution failed because no message dispatcher was configured.");
            attempt.AddError("No message dispatcher was configured in the application settings.");
            return Result.Failure;
        }

        var parameters = step.StepExecutionParameters.ToStringDictionary();

        var recipients = step.GetRecipientsAsList().Select(r => r.Replace(parameters));
        var subject = step.Subject.Replace(parameters);
        var body = step.Body.Replace(parameters);

        await _messageDispatcher.SendMessageAsync(recipients, subject, body, false, cancellationToken);

        return Result.Success;
    }
}
