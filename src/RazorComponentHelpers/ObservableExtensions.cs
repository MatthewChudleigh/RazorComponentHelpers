using System.Runtime.CompilerServices;
using System.Threading.Channels;

namespace RazorComponentHelpers;

public static class ObservableExtensions
{
    public static async IAsyncEnumerable<T> ToAsyncEnumerable<T>(
        this IObservable<T> source,
        [EnumeratorCancellation] CancellationToken cancel = default)
    {
        var channelObserver = new ChannelObserver<T>();
   
        var sub = source.Subscribe(channelObserver);
   
        await using var _ = cancel.Register(() =>
        {
            sub.Dispose();
            channelObserver.OnCompleted();
        });
   
        await foreach (var item in channelObserver.ReadAllAsync(cancel))
            yield return item;
    }

    private class ChannelObserver<T> : IObserver<T>
    {
        private readonly Channel<T> _channel = Channel.CreateUnbounded<T>();

        public IAsyncEnumerable<T> ReadAllAsync(CancellationToken cancel)
        {
            return  _channel.Reader.ReadAllAsync(cancel);
        }

        public void OnNext(T value)
        {
            _channel.Writer.TryWrite(value);
        }
        public void OnError(Exception error)
        {
            _channel.Writer.TryComplete(error);
        }
        public void OnCompleted()
        {
            _channel.Writer.TryComplete();
        }

    }
}