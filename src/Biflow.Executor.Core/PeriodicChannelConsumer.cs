using System.Threading.Channels;
using Microsoft.Extensions.Logging;

namespace Biflow.Executor.Core;

/// <summary>
/// <c>PeriodicChannelConsumer</c> is designed for consuming data from a channel reader with a periodic interval.
/// It buffers data read from the channel and invokes a specified action at each interval or when the channel completes.
/// The class ensures proper handling of cancellation tokens and resource cleanup upon disposal.
/// </summary>
/// <typeparam name="T">Type of data objects being read from the channel.</typeparam>
/// <remarks>
/// The consumer starts periodic consumption when <c>StartConsumingAsync</c> is called,
/// and it continues until the provided cancellation token is triggered.
/// At regular intervals, buffered data is passed to the <c>bufferPublished</c> callback function.
/// Any remaining data in the buffer is flushed when reading completes.
/// </remarks>
public class PeriodicChannelConsumer<T>(
    ILogger logger,
    ChannelReader<T> reader,
    Func<int, TimeSpan> interval,
    Func<IReadOnlyList<T>, CancellationToken, Task> bufferPublished,
    bool enableLastInFirstOut = false,
    int? bufferCapacity = null) : IDisposable
{
    private readonly Lock _bufferLock = new();
    private CancellationTokenSource? _cts;

    /// <summary>
    /// Starts consuming data from the channel and processes it periodically based on the provided interval.
    /// Merges channel reading and timed operations into an asynchronous loop until the cancellation token is triggered.
    /// </summary>
    /// <param name="token">A cancellation token to stop the consuming process.</param>
    /// <returns>A task that represents the asynchronous consuming operation.</returns>
    public async Task StartConsumingAsync(CancellationToken token)
    {
        _cts = CancellationTokenSource.CreateLinkedTokenSource(token);
        var buffer = new List<T>();
        var readTask = ReadFromChannelAsync(buffer, _cts.Token);
        var looperTask = LoopTimerTicksAsync(buffer, _cts.Token);
        await Task.WhenAll(readTask, looperTask);
        
        // Final flush
        if (buffer.Count > 0)
        {
            await bufferPublished(buffer, _cts.Token);
        }
    }

    /// <summary>
    /// Cancels the ongoing operation by triggering the cancellation token source associated with the consumer.
    /// This stops any active or scheduled consumption of the channel data.
    /// </summary>
    public void Cancel() => _cts?.Cancel();
    
    private async Task ReadFromChannelAsync(List<T> buffer, CancellationToken token)
    {
        try
        {
            await foreach (var item in reader.ReadAllAsync(token))
            {
                lock (_bufferLock)
                {
                    if (bufferCapacity is { } cap && buffer.Count >= cap)
                    {
                        // Using FIFO principle, discard new items because the buffer is full. 
                        if (!enableLastInFirstOut)
                            continue;
                        
                        // Using LIFO principle, discard old items because the buffer is full.
                        while (buffer.Count >= cap) buffer.RemoveAt(0);
                    }
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
    
    private async Task LoopTimerTicksAsync(List<T> buffer, CancellationToken token)
    {
        try
        {
            var iteration = 1;
            using var timer = new PeriodicTimer(interval(iteration));
            while (await timer.WaitForNextTickAsync(token))
            {
                if (buffer.Count == 0)
                {
                    continue;
                }
                
                try
                {
                    // Copy the buffer to avoid concurrency issues while
                    // the buffer might be iterated over in the callback.
                    T[] bufferCopy;
                    lock (_bufferLock)
                    {
                        bufferCopy = [..buffer];
                    }
                    await bufferPublished(bufferCopy, token);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error while running buffer publish delegate");
                    continue;
                }

                lock (_bufferLock)
                {
                    buffer.Clear();
                }

                timer.Period = interval(++iteration);
            }
        }
        catch (OperationCanceledException)
        {
            // Graceful exit
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in timer tick loop");
        }
    }

    public void Dispose()
    {
        _cts?.Dispose();
        GC.SuppressFinalize(this);
    }
}