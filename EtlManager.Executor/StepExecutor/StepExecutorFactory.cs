using EtlManager.DataAccess.Models;
using Microsoft.Extensions.DependencyInjection;

namespace EtlManager.Executor;

public class StepExecutorFactory : IStepExecutorFactory
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
            FunctionStepExecution function => ActivatorUtilities.CreateInstance<FunctionStepExecutor>(_serviceProvider, function),
            AgentJobStepExecution agent => ActivatorUtilities.CreateInstance<AgentJobStepExecutor>(_serviceProvider, agent),
            TabularStepExecution tabular => ActivatorUtilities.CreateInstance<TabularStepExecutor>(_serviceProvider, tabular),
            _ => throw new InvalidOperationException($"{stepExecution.StepType} is not a recognized step type")
        };
        return stepExecutor;
    }
}
