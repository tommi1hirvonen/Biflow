using Biflow.DataAccess.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Biflow.Executor.Core.Orchestrator;

internal class JobOrchestratorFactory : IJobOrchestratorFactory
{
    private readonly IServiceProvider _serviceProvider;

    public JobOrchestratorFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public JobOrchestrator Create(Execution execution) =>
        ActivatorUtilities.CreateInstance<JobOrchestrator>(_serviceProvider, execution);
}
