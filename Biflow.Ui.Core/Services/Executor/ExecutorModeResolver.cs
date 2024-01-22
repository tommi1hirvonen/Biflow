namespace Biflow.Ui.Core;

public class ExecutorModeResolver
{
    internal ExecutorModeResolver(ExecutorMode executorMode)
    {
        ExecutorMode = executorMode;
    }

    public ExecutorMode ExecutorMode { get; }
}
