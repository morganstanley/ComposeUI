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

namespace MorganStanley.ComposeUI.ModuleLoader.Tests.TestUtils;

public static class ProcessExtensions
{
    public static async Task<ProcessResult> WaitForExitAsync(
        this Process process,
        TimeSpan timeout)
    {
        var output = "";
        var error = "";
        var cts = new CancellationTokenSource(timeout);

        await Task.WhenAll(ReadOutput(), ReadError(), WaitForExit());

        return new ProcessResult(output, error, process.ExitCode);

        async Task ReadOutput()
        {
            if (!process.StartInfo.RedirectStandardOutput) return;

            output = await process.StandardOutput.ReadToEndAsync().WaitAsync(cts.Token);
        }

        async Task ReadError()
        {
            if (!process.StartInfo.RedirectStandardError) return;
            error = await process.StandardError.ReadToEndAsync().WaitAsync(cts.Token);
        }

        async Task WaitForExit()
        {
            await process.WaitForExitAsync(cts.Token);
        }
    }

    public readonly record struct ProcessResult(string Output, string Error, int ExitCode);
}