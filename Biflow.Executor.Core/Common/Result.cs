namespace Biflow.Executor.Core.Common;

public abstract class Result
{
    public string? InfoMessage { get; }

    public string? WarningMessage { get; }

    protected Result(string? infoMessage, string? warningMessage)
    {
        InfoMessage = string.IsNullOrWhiteSpace(infoMessage) ? null : infoMessage;
        WarningMessage = string.IsNullOrWhiteSpace(warningMessage) ? null : warningMessage;
    }

    public static Success Success(string? infoMessage = null, string? warningMessage = null) =>
        new(infoMessage, warningMessage);

    public static Failure Failure(string errorMessage, string? warningMessage = null, string? infoMessage = null) =>
        new(errorMessage, warningMessage, infoMessage);

}

public class Success : Result
{
    internal Success(string? infoMessage, string? warningMessage) : base(infoMessage, warningMessage)
    {
    }
}

public class Failure : Result
{
    public string ErrorMessage { get; }

    internal Failure(string errorMessage, string? warningMessage, string? infoMessage) : base(infoMessage, warningMessage)
    {
        ErrorMessage = errorMessage;
    }
}
