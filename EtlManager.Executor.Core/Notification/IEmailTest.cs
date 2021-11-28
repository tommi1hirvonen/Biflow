namespace EtlManager.Executor.Core.Notification;

public interface IEmailTest
{
    public Task RunAsync(string toAddress);
}
