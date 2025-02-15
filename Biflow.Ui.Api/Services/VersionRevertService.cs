using System.Collections.Concurrent;
using System.Threading.Channels;

namespace Biflow.Ui.Api.Services;

internal class VersionRevertService(
    ILogger<VersionRevertService> logger,
    VersionRevertJobDictionary statuses) : BackgroundService
{
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private readonly Channel<VersionRevertJob> _channel =
        Channel.CreateBounded<VersionRevertJob>(new BoundedChannelOptions(1) { SingleReader = true });

    public bool TryEnqueue(VersionRevertJob job) => _semaphore.CurrentCount > 0 && _channel.Writer.TryWrite(job);
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var job in _channel.Reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                await _semaphore.WaitAsync(stoppingToken);
                statuses[job.Id] = new VersionRevertJobState(VersionRevertJobStatus.Processing, []);
                var response = await job.TaskDelegate(stoppingToken);
                var integrations = response.NewIntegrations
                    .Select(x => (x.Type.Name, x.Name))
                    .ToArray();
                statuses[job.Id] = new VersionRevertJobState(VersionRevertJobStatus.Completed, integrations);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                statuses[job.Id] = new VersionRevertJobState(VersionRevertJobStatus.Failed, []);
                logger.LogError(ex, "Error reverting environment version");
            }
            finally
            {
                _semaphore.Release();
            }
        }
    }
}