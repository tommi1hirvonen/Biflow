namespace Biflow.Executor.Core.StepExecutor;

internal abstract class Message
{
}

internal class Output : Message
{
    public string Message { get; }

    public Output(string message)
    {
        Message = message;
    }
}

internal class Warning : Message
{
    public Exception? Exception { get; }

    public string Message { get; }

    public Warning(Exception? exception, string message)
    {
        Exception = exception;
        Message = message;
    }
}
