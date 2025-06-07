using Microsoft.Extensions.DependencyInjection;

namespace Biflow.Executor.Core.StepExecutor;

internal class StepExecutorProvider(IServiceProvider serviceProvider) : IStepExecutorProvider
{
    public IStepExecutor GetExecutorFor(StepExecution step, StepExecutionAttempt attempt)
    {
        var type = typeof(IStepExecutor<,>).MakeGenericType(step.GetType(), attempt.GetType());
        var executor = serviceProvider.GetRequiredService(type);
        return (IStepExecutor)executor;
    }
}
