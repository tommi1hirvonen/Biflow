using Biflow.DataAccess.Models;

namespace Biflow.Executor.Core.Orchestrator;

internal record StepExecutionStatusInfo(StepExecution StepExecution, OrchestrationStatus Status);
