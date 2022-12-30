namespace Biflow.Executor.Core.StepExecutor;

internal abstract class Result
{
    protected Result()
    {
    }

    public static Success Success() => new SuccessImpl();

    public static Failure Failure(Exception exception, string errorMessage) => new FailureImpl(exception, errorMessage);

    public static Failure Failure(string errorMessage) => new FailureImpl(null, errorMessage);

    // Use private concrete classes to prevent calling Success and Failure constructors outside of this class hierarchy.
    // Object instantiation should be done using the static methods above.
    private class SuccessImpl : Success
    {
        public SuccessImpl() : base() { }
    }

    private class FailureImpl : Failure
    {
        public FailureImpl(Exception? exception, string errorMessage) : base(exception, errorMessage) { }
    }
}

internal abstract class Success : Result
{
    public Success() : base()
    {
    }
}

internal abstract class Failure : Result
{
    public Exception? Exception { get; }

    public string ErrorMessage { get; }

    internal Failure(Exception? exception, string errorMessage) : base()
    {
        Exception = exception;
        ErrorMessage = errorMessage;
    }
}
