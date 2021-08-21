using EtlManagerDataAccess.Models;

namespace EtlManagerExecutor
{
    public interface IStepExecutorFactory
    {
        StepExecutorBase Create(StepExecution stepExecution);
    }
}