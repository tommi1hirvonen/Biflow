using EtlManager.DataAccess.Models;

namespace EtlManager.Executor.Core.StepExecutor;

internal interface IStepExecutorFactory
{
    StepExecutorBase Create(StepExecution stepExecution);
}
