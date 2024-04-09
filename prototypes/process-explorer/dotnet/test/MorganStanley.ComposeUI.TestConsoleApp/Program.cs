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

Console.WriteLine("Hello, World!");

var stopwatch = Stopwatch.StartNew();

Console.WriteLine("This is a test application to test the ProcessMonitor...");
Console.WriteLine("Starting a process...");

var childProcess = Process.Start(
    new ProcessStartInfo
    {
        FileName = Path.GetFullPath($"../../../../dotnet/test/MorganStanley.ComposeUI.TestConsoleApp2/net6.0/MorganStanley.ComposeUI.TestConsoleApp2.exe"),
        RedirectStandardError = true,
        RedirectStandardOutput = true,
    });

if (childProcess == null) throw new NullReferenceException(nameof(childProcess));

using (var streamReader = childProcess.StandardOutput)
{
    var line = streamReader.ReadLine();

    while (!streamReader.EndOfStream)
    {
        if (line == null)
        {
            line = await streamReader.ReadLineAsync();
        }
        else if (line.Contains("Hello world from ProcessExplorerTestApp2!"))
        {
            Console.WriteLine(line);
            break;
        }
        else if (line != null)
        {
            Console.WriteLine(line);
        }
    }
}

Thread.Sleep(5000);

Console.WriteLine("Terminating a process....");
childProcess?.Kill();
stopwatch.Stop();
Console.WriteLine("ChildProcess is terminated");
