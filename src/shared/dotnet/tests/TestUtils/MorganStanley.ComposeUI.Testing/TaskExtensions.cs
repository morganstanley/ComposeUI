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

// ReSharper disable UnusedMember.Global

using System.Diagnostics;
using Xunit.Sdk;

namespace MorganStanley.ComposeUI.Testing;

public static class TaskExtensions
{
    [Obsolete("Don't use, this method is not reliable. Add proper instrumentation to your code instead.")]
    public static async Task WaitForBackgroundTasksAsync(CancellationToken cancellationToken = default)
    {
        // Quick and dirty method of waiting for background tasks to finish.
        // We try to schedule enough tasks so that the thread pool is fully utilized.
        ThreadPool.GetMaxThreads(out var workerThreads, out var completionPortThreads);
        var taskCount = workerThreads + completionPortThreads;
        var gate = new SemaphoreSlim(0);

        // Schedule a batch of blocking tasks
        var task = Task.WhenAll(
            Enumerable.Range(0, taskCount)
                .Select(async _ => await gate.WaitAsync(cancellationToken)));

        // Let the tasks complete
        await Task.Yield();
        gate.Release(taskCount);

        await task;

        if (SynchronizationContext.Current is AsyncTestSyncContext asyncContext)
        {
            await asyncContext.WaitForCompletionAsync();
        }
    }

    [Obsolete("Don't use, this method is not reliable. Add proper instrumentation to your code instead.")]
    public static async Task WaitForBackgroundTasksAsync(TimeSpan minimumWaitTime, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        await WaitForBackgroundTasksAsync(cancellationToken);
        minimumWaitTime -= stopwatch.Elapsed;
        if (minimumWaitTime > TimeSpan.Zero)
        {
            await Task.Delay(minimumWaitTime, cancellationToken);
        }
    }
}