using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using RazorComponentHelpers;

namespace RazorComponentHelpers.Test;

public class ObservableExtensionsTest
{
    [Fact]
    public async Task ToAsyncEnumerable_ForwardsValuesAndCompletes()
    {
        var observable = new TestObservable<int>();

        var enumerationTask = Task.Run(async () =>
        {
            var collected = new List<int>();
            await foreach (var item in observable.ToAsyncEnumerable())
            {
                collected.Add(item);
            }

            return collected;
        });

        await WaitForAsync(observable.WaitForSubscriptionAsync, "The observable was not subscribed.");

        observable.Emit(1);
        observable.Emit(2);
        observable.Emit(3);
        observable.Complete();

        var result = await enumerationTask;

        Assert.Equal(new[] { 1, 2, 3 }, result);
    }

    [Fact]
    public async Task ToAsyncEnumerable_CancelsSubscriptionWhenTokenIsCancelled()
    {
        var observable = new TestObservable<int>();
        using var cts = new CancellationTokenSource();

        var enumerationTask = Task.Run(async () =>
        {
            await foreach (var _ in observable.ToAsyncEnumerable(cts.Token))
            {
                // Intentionally empty - waiting for cancellation to end enumeration.
            }
        });

        await WaitForAsync(observable.WaitForSubscriptionAsync, "The observable was not subscribed.");

        cts.Cancel();

        Exception? observedException = null;
        try
        {
            await enumerationTask;
        }
        catch (Exception ex)
        {
            observedException = ex;
        }

        Assert.True(observedException is null or OperationCanceledException);

        await WaitForAsync(observable.WaitForDisposeAsync, "The subscription was not disposed after cancellation.");
    }

    [Fact]
    public async Task ToAsyncEnumerable_PropagatesErrors()
    {
        var observable = new TestObservable<int>();

        var enumerationTask = Task.Run(async () =>
        {
            var collected = new List<int>();
            await foreach (var item in observable.ToAsyncEnumerable())
            {
                collected.Add(item);
            }

            return collected;
        });

        await WaitForAsync(observable.WaitForSubscriptionAsync, "The observable was not subscribed.");

        var expectedException = new InvalidOperationException("boom");

        observable.Emit(42);
        observable.Error(expectedException);

        var actual = await Assert.ThrowsAsync<InvalidOperationException>(async () => await enumerationTask);

        Assert.Same(expectedException, actual);
    }

    private static async Task WaitForAsync(Task task, string failureMessage, int timeoutMilliseconds = 1000)
    {
        var completedTask = await Task.WhenAny(task, Task.Delay(timeoutMilliseconds));
        Assert.True(completedTask == task, failureMessage);
        await task;
    }

    private sealed class TestObservable<T> : IObservable<T>
    {
        private readonly List<IObserver<T>> _observers = new();
        private readonly object _gate = new();
        private readonly TaskCompletionSource<bool> _subscribed = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource<bool> _disposed = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task WaitForSubscriptionAsync => _subscribed.Task;
        public Task WaitForDisposeAsync => _disposed.Task;

        public IDisposable Subscribe(IObserver<T> observer)
        {
            lock (_gate)
            {
                _observers.Add(observer);
            }

            _subscribed.TrySetResult(true);

            return new Subscription(this, observer, _disposed);
        }

        public void Emit(T value)
        {
            foreach (var observer in Snapshot())
            {
                observer.OnNext(value);
            }
        }

        public void Complete()
        {
            foreach (var observer in Snapshot(clear: true))
            {
                observer.OnCompleted();
            }
        }

        public void Error(Exception exception)
        {
            foreach (var observer in Snapshot(clear: true))
            {
                observer.OnError(exception);
            }
        }

        private List<IObserver<T>> Snapshot(bool clear = false)
        {
            lock (_gate)
            {
                var snapshot = new List<IObserver<T>>(_observers);
                if (clear)
                {
                    _observers.Clear();
                }

                return snapshot;
            }
        }

        private void Remove(IObserver<T> observer)
        {
            lock (_gate)
            {
                _observers.Remove(observer);
            }
        }

        private sealed class Subscription : IDisposable
        {
            private readonly TestObservable<T> _parent;
            private readonly IObserver<T> _observer;
            private readonly TaskCompletionSource<bool> _disposed;
            private int _isDisposed;

            public Subscription(TestObservable<T> parent, IObserver<T> observer, TaskCompletionSource<bool> disposed)
            {
                _parent = parent;
                _observer = observer;
                _disposed = disposed;
            }

            public void Dispose()
            {
                if (Interlocked.Exchange(ref _isDisposed, 1) == 1)
                {
                    return;
                }

                _parent.Remove(_observer);
                _disposed.TrySetResult(true);
            }
        }
    }
}
