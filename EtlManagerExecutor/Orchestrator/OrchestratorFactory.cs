using EtlManagerDataAccess.Models;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EtlManagerExecutor
{
    public class OrchestratorFactory : IOrchestratorFactory
    {
        private readonly IServiceProvider _serviceProvider;

        public OrchestratorFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public OrchestratorBase Create(Execution execution)
        {
            OrchestratorBase orchestrator;
            if (execution.DependencyMode)
            {
                Log.Information("{ExecutionId} Created orchestrator in dependency mode", execution.ExecutionId);
                orchestrator = ActivatorUtilities.CreateInstance<DependencyModeOrchestrator>(_serviceProvider, execution);
            }
            else
            {
                Log.Information("{executionId} Created orchestrator in execution phase mode", execution.ExecutionId);
                orchestrator = ActivatorUtilities.CreateInstance<ExecutionPhaseOrchestrator>(_serviceProvider, execution);
            }
            return orchestrator;
        }
    }
}
