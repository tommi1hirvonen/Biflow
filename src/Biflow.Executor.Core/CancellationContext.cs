namespace Biflow.Executor.Core;

internal class CancellationContext(UserCancellationTokenSource userCancellation, CancellationToken shutdownToken)
{
    public string Username => userCancellation.Username;
    
    /// <summary>
    /// The CancellationToken will be canceled when the user cancels the execution or when service shutdown is initiated.
    /// </summary>
    public CancellationToken CancellationToken => userCancellation.Token;
    
    /// <summary>
    /// ShutdownToken will be canceled when the service is shutting down.
    /// </summary>
    public CancellationToken ShutdownToken { get; } = shutdownToken;
    
    public bool IsCancellationRequested =>
        userCancellation.IsCancellationRequested || ShutdownToken.IsCancellationRequested;
}