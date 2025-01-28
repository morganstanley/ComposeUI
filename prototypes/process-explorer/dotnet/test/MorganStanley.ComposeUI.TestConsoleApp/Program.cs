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
using MorganStanley.ComposeUI.TestConsoleApp;

Console.WriteLine("Hello, World!");
var stopwatch = Stopwatch.StartNew();
Console.WriteLine("This is a test application to test the ProcessMonitor...");

Console.WriteLine("Starting a process...");

var isDebug = false;
Helper.IsDebug(ref isDebug);

var folder = isDebug ? "Debug" : "Release";
Console.WriteLine(folder);

var childProcess = Process.Start(Path.GetFullPath($"../../../../MorganStanley.ComposeUI.TestConsoleApp2/bin/{folder}/net8.0/MorganStanley.ComposeUI.TestConsoleApp2.exe"));

var sum = 0;
for (int i = 0; i < 50000000; i++)
{
    sum += i;
}
Thread.Sleep(10000);

Console.WriteLine("Terminating a process....");
childProcess.Kill();
stopwatch.Stop();
Console.WriteLine("ChildProcess is terminated");
