namespace Biflow.Executor.Core;

internal class UserCancellationTokenSource : CancellationTokenSource
{
    public string Username { get; private set; } = "timeout";

    public void Cancel(string username)
    {
        Username = username;
        Cancel();
    }
}
