using EtlManager.DataAccess.Models;

namespace EtlManager.Executor;

public interface IStepExecutorFactory
{
    StepExecutorBase Create(StepExecution stepExecution);
}
