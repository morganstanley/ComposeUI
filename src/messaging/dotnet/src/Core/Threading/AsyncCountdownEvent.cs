// Morgan Stanley makes this available to you under the Apache License,
// Version 2.0 (the "License"). You may obtain a copy of the License at
// 
//      http://www.apache.org/licenses/LICENSE-2.0.
// 
// See the NOTICE file distributed with this work for additional information
// regarding copyright ownership. Unless required by applicable law or agreed
// to in writing, software distributed under the License is distributed on an
// "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express
// or implied. See the License for the specific language governing permissions
// and limitations under the License.

using System.Diagnostics;

namespace MorganStanley.ComposeUI.Messaging.Threading;

/// <summary>
/// A coordination primitive that allows multiple tasks to signal completion,
/// with the ability to wait for all signals to be received.
/// </summary>
public sealed class AsyncCountdownEvent : IDisposable
{
    private readonly object _lock = new();
    private int _count;
    private TaskCompletionSource<bool> _tcs;
    private bool _disposed;

    public AsyncCountdownEvent(int initialCount)
    {
        if (initialCount < 0)
            throw new ArgumentOutOfRangeException(nameof(initialCount));
        
        _count = initialCount;
        _tcs = new TaskCompletionSource<bool>();
        
        if (initialCount == 0)
            _tcs.SetResult(true);
    }

    public int CurrentCount
    {
        get
        {
            lock (_lock)
            {
                return _count;
            }
        }
    }

    public bool IsSet
    {
        get
        {
            lock (_lock)
            {
                return _count == 0;
            }
        }
    }

    public Task WaitAsync()
    {
        lock (_lock)
        {
            ThrowIfDisposed();
            return _tcs.Task;
        }
    }

    public Task WaitAsync(CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
            return Task.FromCanceled(cancellationToken);

        lock (_lock)
        {
            ThrowIfDisposed();
            var task = _tcs.Task;
            
            if (task.IsCompleted || !cancellationToken.CanBeCanceled)
                return task;

            return WaitAsyncWithCancellation(task, cancellationToken);
        }
    }

    private static async Task WaitAsyncWithCancellation(Task task, CancellationToken cancellationToken)
    {
        var tcs = new TaskCompletionSource<bool>();
        using var registration = cancellationToken.Register(() => tcs.TrySetCanceled(cancellationToken));
        
        var completedTask = await Task.WhenAny(task, tcs.Task);
        if (completedTask == tcs.Task)
            cancellationToken.ThrowIfCancellationRequested();
        
        await task;
    }

    public void Signal()
    {
        Signal(1);
    }

    public void Signal(int signalCount)
    {
        if (signalCount <= 0)
            throw new ArgumentOutOfRangeException(nameof(signalCount));

        TaskCompletionSource<bool>? tcsToComplete = null;

        lock (_lock)
        {
            ThrowIfDisposed();
            
            if (_count == 0)
                throw new InvalidOperationException("The event is already set.");

            _count -= signalCount;
            
            if (_count < 0)
                throw new InvalidOperationException("Signal count would cause the current count to be negative.");

            if (_count == 0)
                tcsToComplete = _tcs;
        }

        tcsToComplete?.SetResult(true);
    }

    public void AddCount()
    {
        AddCount(1);
    }

    public void AddCount(int signalCount)
    {
        if (signalCount <= 0)
            throw new ArgumentOutOfRangeException(nameof(signalCount));

        lock (_lock)
        {
            ThrowIfDisposed();
            
            if (_count == 0)
            {
                // Reset the TaskCompletionSource since we're going from 0 to non-zero
                _tcs = new TaskCompletionSource<bool>();
            }

            _count += signalCount;
        }
    }

    public void Reset()
    {
        Reset(0);
    }

    public void Reset(int count)
    {
        if (count < 0)
            throw new ArgumentOutOfRangeException(nameof(count));

        lock (_lock)
        {
            ThrowIfDisposed();
            
            _count = count;
            _tcs = new TaskCompletionSource<bool>();
            
            if (count == 0)
                _tcs.SetResult(true);
        }
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
            throw new ObjectDisposedException(GetType().Name);
    }

    public void Dispose()
    {
        lock (_lock)
        {
            if (_disposed)
                return;

            _disposed = true;
            _tcs.TrySetCanceled();
        }
    }
}
