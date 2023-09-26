using System.Threading.Tasks.Dataflow;

namespace Biflow.Executor.Core.WebExtensions;

internal class AsyncQueue<T> : IAsyncEnumerable<T>
{
    private readonly SemaphoreSlim _enumerationSemaphore = new(1);
    private readonly BufferBlock<T> _bufferBlock = new();

    public void Enqueue(T item) => _bufferBlock.Post(item);

    public async IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken token = default)
    {
        // Synchronize access to the queue in case multiple foreach loops try to compete for items.
        await _enumerationSemaphore.WaitAsync(token);
        try
        {
            // Yield new elements until cancellation is triggered.
            while (true)
            {
                // Throw in case of cancellation to transfer the Task to canceled state.
                token.ThrowIfCancellationRequested();
                yield return await _bufferBlock.ReceiveAsync(token);
            }
        }
        finally
        {
            _enumerationSemaphore.Release();
        }
    }
}