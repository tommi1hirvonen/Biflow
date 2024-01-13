using Microsoft.Extensions.DependencyInjection;

namespace Biflow.Executor.Core.Orchestrator;

internal class JobOrchestratorFactory(IServiceProvider serviceProvider) : IJobOrchestratorFactory
{
    private readonly IServiceProvider _serviceProvider = serviceProvider;

    public IJobOrchestrator Create(Execution execution) =>
        ActivatorUtilities.CreateInstance<JobOrchestrator>(_serviceProvider, execution);
}
