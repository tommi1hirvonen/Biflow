using Biflow.DataAccess.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Biflow.Executor.Core.StepExecutor;

internal class StepExecutorFactory : IStepExecutorFactory
{
    private readonly IServiceProvider _serviceProvider;

    public StepExecutorFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public StepExecutorBase Create(StepExecution stepExecution)
    {
        StepExecutorBase stepExecutor = stepExecution switch
        {
            SqlStepExecution sql => ActivatorUtilities.CreateInstance<SqlStepExecutor>(_serviceProvider, sql),
            PackageStepExecution package => ActivatorUtilities.CreateInstance<PackageStepExecutor>(_serviceProvider, package),
            JobStepExecution job => ActivatorUtilities.CreateInstance<JobStepExecutor>(_serviceProvider, job),
            PipelineStepExecution pipeline => ActivatorUtilities.CreateInstance<PipelineStepExecutor>(_serviceProvider, pipeline),
            ExeStepExecution exe => ActivatorUtilities.CreateInstance<ExeStepExecutor>(_serviceProvider, exe),
            DatasetStepExecution dataset => ActivatorUtilities.CreateInstance<DatasetStepExecutor>(_serviceProvider, dataset),
            FunctionStepExecution durable and { FunctionIsDurable: true } => ActivatorUtilities.CreateInstance<DurableFunctionStepExecutor>(_serviceProvider, durable),
            FunctionStepExecution function => ActivatorUtilities.CreateInstance<FunctionStepExecutor>(_serviceProvider, function),
            AgentJobStepExecution agent => ActivatorUtilities.CreateInstance<AgentJobStepExecutor>(_serviceProvider, agent),
            TabularStepExecution tabular => ActivatorUtilities.CreateInstance<TabularStepExecutor>(_serviceProvider, tabular),
            EmailStepExecution email => ActivatorUtilities.CreateInstance<EmailStepExecutor>(_serviceProvider, email),
            QlikStepExecution qlik => ActivatorUtilities.CreateInstance<QlikStepExecutor>(_serviceProvider, qlik),
            _ => throw new InvalidOperationException($"{stepExecution.StepType} is not a recognized step type")
        };
        return stepExecutor;
    }
}
