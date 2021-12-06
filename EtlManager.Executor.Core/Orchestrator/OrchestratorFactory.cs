using EtlManager.DataAccess.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace EtlManager.Executor.Core.Orchestrator;

internal class OrchestratorFactory : IOrchestratorFactory
{
    private readonly ILogger<OrchestratorFactory> _logger;
    private readonly IServiceProvider _serviceProvider;

    public OrchestratorFactory(ILogger<OrchestratorFactory> logger, IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    public OrchestratorBase Create(Execution execution)
    {
        OrchestratorBase orchestrator;
        if (execution.DependencyMode)
        {
            _logger.LogInformation("{ExecutionId} Created orchestrator in dependency mode", execution.ExecutionId);
            orchestrator = ActivatorUtilities.CreateInstance<DependencyModeOrchestrator>(_serviceProvider, execution);
        }
        else
        {
            _logger.LogInformation("{executionId} Created orchestrator in execution phase mode", execution.ExecutionId);
            orchestrator = ActivatorUtilities.CreateInstance<ExecutionPhaseOrchestrator>(_serviceProvider, execution);
        }
        return orchestrator;
    }
}
