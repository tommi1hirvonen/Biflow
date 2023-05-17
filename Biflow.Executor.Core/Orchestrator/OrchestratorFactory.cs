using Biflow.DataAccess.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Biflow.Executor.Core.Orchestrator;

internal class OrchestratorFactory : IOrchestratorFactory
{
    private readonly ILogger<OrchestratorFactory> _logger;
    private readonly IServiceProvider _serviceProvider;

    public OrchestratorFactory(ILogger<OrchestratorFactory> logger, IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    // TODO Handle execution phase mode orchestrator instantiation
    public JobOrchestrator Create(Execution execution) =>
        ActivatorUtilities.CreateInstance<JobOrchestrator>(_serviceProvider, execution);

}
