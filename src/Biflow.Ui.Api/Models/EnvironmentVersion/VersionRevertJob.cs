namespace Biflow.Ui.Api.Models;

internal class VersionRevertJob(Func<CancellationToken, Task<RevertVersionResponse>> taskDelegate)
{
    public Guid Id { get; } = Guid.NewGuid();
    
    public Func<CancellationToken, Task<RevertVersionResponse>> TaskDelegate { get; } = taskDelegate;
}