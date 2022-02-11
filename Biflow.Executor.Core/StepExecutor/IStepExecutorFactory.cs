using Biflow.DataAccess.Models;

namespace Biflow.Executor.Core.StepExecutor;

internal interface IStepExecutorFactory
{
    StepExecutorBase Create(StepExecution stepExecution);
}
