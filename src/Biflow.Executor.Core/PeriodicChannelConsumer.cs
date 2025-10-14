using System.Threading.Channels;
using Microsoft.Extensions.Logging;

namespace Biflow.Executor.Core;

public class PeriodicChannelConsumer<T>(
    ILogger logger,
    ChannelReader<T> reader,
    TimeSpan interval,
    Func<IReadOnlyList<T>, CancellationToken, Task> bufferPublished) : IDisposable
{
    private readonly Lock _bufferLock = new();
    private CancellationTokenSource? _cts;
    
    public async Task StartConsumingAsync(CancellationToken token)
    {
        _cts = CancellationTokenSource.CreateLinkedTokenSource(token);
        var buffer = new List<T>();
        using var timer = new PeriodicTimer(interval);
        var readTask = ReadFromChannelAsync(buffer, _cts.Token);
        var looperTask = LoopTimerTicksAsync(timer, buffer, _cts.Token);
        await Task.WhenAll(readTask, looperTask);
        
        // Final flush
        if (buffer.Count > 0)
        {
            await bufferPublished(buffer, _cts.Token);
        }
    }

    public void Cancel() => _cts?.Cancel();
    
    private async Task ReadFromChannelAsync(List<T> buffer, CancellationToken token)
    {
        try
        {
            await foreach (var item in reader.ReadAllAsync(token))
            {
                lock (_bufferLock)
                {
                    buffer.Add(item);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Graceful cancellation
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error while reading from output channel");
        }
    }
    
    private async Task LoopTimerTicksAsync(PeriodicTimer timer, List<T> buffer, CancellationToken token)
    {
        try
        {
            while (await timer.WaitForNextTickAsync(token))
            {
                if (buffer.Count == 0)
                {
                    continue;
                }
                
                try
                {
                    await bufferPublished(buffer, token);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error in output update loop");
                    continue;
                }

                lock (_bufferLock)
                {
                    buffer.Clear();
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Graceful exit
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in output update loop");
        }
    }

    public void Dispose()
    {
        _cts?.Dispose();
        GC.SuppressFinalize(this);
    }
}