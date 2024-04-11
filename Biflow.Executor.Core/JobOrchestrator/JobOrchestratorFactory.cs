using Microsoft.Extensions.DependencyInjection;

namespace Biflow.Executor.Core.JobOrchestrator;

internal class JobOrchestratorFactory(IServiceProvider serviceProvider) : IJobOrchestratorFactory
{
    private readonly IServiceProvider _serviceProvider = serviceProvider;

    public IJobOrchestrator Create(Execution execution) =>
        ActivatorUtilities.CreateInstance<JobOrchestrator>(_serviceProvider, execution);
}
