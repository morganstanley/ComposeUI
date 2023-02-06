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

namespace ProcessExplorer.Core.Extensions;

internal static class ProcessExtensions
{
    public static Task WaitForExitAsync(this Process process, CancellationToken cancellationToken)
    {
        var tcs = new TaskCompletionSource<object>();
        process.EnableRaisingEvents = true;
        process.Exited += (sender, e) => tcs.TrySetResult(null);
        if (cancellationToken != CancellationToken.None)
        {
            cancellationToken.Register(() => tcs.TrySetCanceled());
        }
        return tcs.Task;
    }
}
