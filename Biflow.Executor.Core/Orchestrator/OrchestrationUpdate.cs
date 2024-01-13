using Biflow.Core.Entities.Steps.Execution;

namespace Biflow.Executor.Core.Orchestrator;

internal record OrchestrationUpdate(StepExecution StepExecution, OrchestrationStatus Status);
