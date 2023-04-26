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

namespace MorganStanley.ComposeUI.Testing;

public static class TaskExtensions
{
    public static Task WaitForBackgroundTasksAsync(CancellationToken cancellationToken = default)
    {
        // Quick and dirty method of waiting for background tasks to finish.
        var taskCount = ThreadPool.ThreadCount * 2;
        var gate = new SemaphoreSlim(0);

        var task = Task.WhenAll(
            Enumerable.Range(0, taskCount)
                .Select(_ => gate.WaitAsync(cancellationToken)));

        Task.Delay(1, cancellationToken).ContinueWith(_ => gate.Release(taskCount), cancellationToken);

        return task;
    }

    public static Task WaitForBackgroundTasksAsync(TimeSpan timeout)
    {
        var cts = new CancellationTokenSource(timeout);
        return WaitForBackgroundTasksAsync(cts.Token);
    }
}