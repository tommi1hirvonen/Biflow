namespace Biflow.Ui.Api.Models;

internal class VersionRevertJob(Func<CancellationToken, Task> taskDelegate)
{
    public Guid Id { get; } = Guid.NewGuid();
    
    public Func<CancellationToken, Task> TaskDelegate { get; } = taskDelegate;
}