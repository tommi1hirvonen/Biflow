namespace Biflow.Executor.Core.StepExecutor;

internal abstract record Message;

internal record Output(string Message) : Message;

internal record Warning(Exception? Exception, string Message) : Message
{
    public Warning(string message) : this(null, message) { }
}