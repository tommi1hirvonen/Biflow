using Biflow.DataAccess.Models;

namespace Biflow.Executor.Core.Orchestrator;

internal record OrchestrationUpdate(StepExecution StepExecution, OrchestrationStatus Status);
