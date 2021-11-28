using EtlManager.DataAccess.Models;

namespace EtlManager.Executor.Core.StepExecutor;

public interface IStepExecutorFactory
{
    StepExecutorBase Create(StepExecution stepExecution);
}
