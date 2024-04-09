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

using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using MorganStanley.ComposeUI.ProcessExplorer.Abstractions.Processes;
using MorganStanley.ComposeUI.ProcessExplorer.Core.Processes;
using MorganStanley.ComposeUI.ProcessExplorer.Core.Tests.Utils;
using Xunit;

namespace MorganStanley.ComposeUI.ProcessExplorer.Core.Tests.Processes;

public class WindowsProcessInfoManagerTests
{
    [Fact]
    public async Task AddChildProcesses_will_add_child_processes_to_the_list()
    {
        var loggerMock = CreateLoggerMock();
        var processMonitor = CreateWindowsProcessMonitor(loggerMock.Object);
        var testApplicationProcess = Process.Start(
            new ProcessStartInfo
            {
                FileName = GetTestApplicationPath(),
                RedirectStandardOutput = true,
            }); //It will start an another child process

        if (testApplicationProcess == null) throw new NullReferenceException(nameof(testApplicationProcess));

        await testApplicationProcess.WaitForMessageOfChildProcess("Hello world from ProcessExplorerTestApp2!"); //State when the ChildProcess is started

        processMonitor.AddChildProcesses(testApplicationProcess.Id, testApplicationProcess.ProcessName);

        var result = processMonitor.GetProcessIds().ToArray();

        Assert.NotEmpty(result);
        Assert.True(result.Length >= 2);
        Assert.Contains(testApplicationProcess.Id, result);

        testApplicationProcess.Kill();
        processMonitor.Dispose();
    }

    [Fact]
    public void AddProcess_will_add_process_to_the_watchable_list()
    {
        var loggerMock = CreateLoggerMock();
        var processMonitor = CreateWindowsProcessMonitor(loggerMock.Object);

        var process = Process.Start(GetSimpleTestApplicationPath());

        processMonitor.AddProcess(process.Id);
        var result = processMonitor.GetProcessIds().ToArray();

        Assert.NotEmpty(result);
        Assert.Single(result);
        Assert.Contains(process.Id, result);

        process.Kill();
        processMonitor.Dispose();
    }

    [Fact]
    public void CheckIfIsComposeProcess_will_check_if_it_is_contained_by_the_list_and_return_true()
    {
        var loggerMock = CreateLoggerMock();
        var processMonitor = CreateWindowsProcessMonitor(loggerMock.Object);
        var process = Process.Start(GetSimpleTestApplicationPath());
        processMonitor.AddProcess(process.Id);

        var result = processMonitor.CheckIfIsComposeProcess(process.Id);

        Assert.True(result);

        process.Kill();
        processMonitor.Dispose();
    }

    [Fact]
    public void CheckIfIsComposeProcess_will_check_if_it_is_contained_by_the_list_and_return_false()
    {
        var loggerMock = CreateLoggerMock();
        var processMonitor = CreateWindowsProcessMonitor(loggerMock.Object);
        var process = Process.Start(GetSimpleTestApplicationPath());

        var result = processMonitor.CheckIfIsComposeProcess(process.Id);

        Assert.False(result);

        process.Kill();
        processMonitor.Dispose();
    }

    [Fact]
    public void ClearProcessIds_will_remove_all_the_elements()
    {
        var loggerMock = CreateLoggerMock();
        var processMonitor = CreateWindowsProcessMonitor(loggerMock.Object);
        var process = Process.Start(GetSimpleTestApplicationPath());
        processMonitor.AddProcess(process.Id);

        processMonitor.ClearProcessIds();
        var result = processMonitor.GetProcessIds().ToArray();

        Assert.Empty(result);

        process.Kill();
        processMonitor.Dispose();
    }

    [Fact] //It is hard to reproduce a process which uses cpu in a time that is being measured in percentage
    public void GetCpuUsage_will_return_with_some_value()
    {
        var loggerMock = CreateLoggerMock();
        var processMonitor = CreateWindowsProcessMonitor(loggerMock.Object);
        var process = Process.GetProcessById(Environment.ProcessId);

        var cpuUsageResult = processMonitor.GetCpuUsage(
            process.Id, 
            process.ProcessName);

        Assert.True(cpuUsageResult >= 0);
        
        processMonitor.Dispose();
    }

    [Fact]
    public void GetMemoryUsage_will_return_with_some_value()
    {
        var loggerMock = CreateLoggerMock();
        var processMonitor = CreateWindowsProcessMonitor(loggerMock.Object);

        var memoryUsageResult = processMonitor.GetMemoryUsage(
            Environment.ProcessId,
            Process.GetProcessById(Environment.ProcessId).ProcessName);

        Assert.True(memoryUsageResult > 0);
        
        processMonitor.Dispose();
    }

    [Fact]
    public void GetParentId_will_return_the_parent_id_of_the_process()
    {
        var loggerMock = CreateLoggerMock();
        var processMonitor = CreateWindowsProcessMonitor(loggerMock.Object);

        var process = Process.Start(GetSimpleTestApplicationPath());

        //Expected parentid should be the current process id, as we are starting this process from the code.
        var parentId = processMonitor.GetParentId(process.Id, process.ProcessName);

        Assert.NotNull(parentId);
        Assert.Equal(Environment.ProcessId, parentId);

        process.Kill();
        processMonitor.Dispose();
    }

    [Fact]
    public void GetParentId_will_return_null()
    {
        var loggerMock = CreateLoggerMock();
        var processMonitor = CreateWindowsProcessMonitor(loggerMock.Object);

        var parentId = processMonitor.GetParentId(666666, string.Empty);

        Assert.Null(parentId);

        processMonitor.Dispose();
    }
    
    [Fact]
    public async Task SetProcessIds_will_set_the_ids_and_its_child_process_ids()
    {
        var loggerMock = CreateLoggerMock();
        var processMonitor = CreateWindowsProcessMonitor(loggerMock.Object);

        var testApplicationProcess = Process.Start(
            new ProcessStartInfo
            {
                FileName = GetTestApplicationPath(),
                RedirectStandardOutput = true,
            }); //It will start an another child process

        if (testApplicationProcess == null) throw new NullReferenceException(nameof(testApplicationProcess));

        await testApplicationProcess.WaitForMessageOfChildProcess("Hello world from ProcessExplorerTestApp2!");

        var processes = new[] { testApplicationProcess.Id };

        // Waiting for the test process to start
        Thread.Sleep(1000);
        processMonitor.SetProcessIds(Environment.ProcessId, processes);

        var result = processMonitor.GetProcessIds().ToArray();

        foreach (var process in processes)
        {
            Assert.Contains(process, result);
        }

        Assert.Equal(3, result.Length);

        testApplicationProcess.Kill();
        processMonitor.Dispose();
    }

    private static Mock<ILogger<ProcessInfoMonitor>> CreateLoggerMock()
    {
        var loggerMock = new Mock<ILogger<ProcessInfoMonitor>>();

        var loggerFilterOptions = new LoggerFilterOptions();

        loggerFilterOptions.AddFilter("", LogLevel.Debug);

        loggerMock
            .Setup(x => x.IsEnabled(It.IsAny<LogLevel>()))
            .Returns<LogLevel>(level => loggerFilterOptions.MinLevel <= level);

        return loggerMock;
    }

    private static ProcessInfoMonitor CreateWindowsProcessMonitor(ILogger<ProcessInfoMonitor> logger)
    {
        var processMonitor = new WindowsProcessInfoMonitor(logger);
        return processMonitor;
    }

    private static string GetTestApplicationPath()
    {
        return Path.GetFullPath($@"../../../../dotnet/test/MorganStanley.ComposeUI.TestConsoleApp/net6.0/MorganStanley.ComposeUI.TestConsoleApp.exe");
    }

    private static string GetSimpleTestApplicationPath()
    {
        return Path.GetFullPath($"../../../../dotnet/test/MorganStanley.ComposeUI.TestConsoleApp2/net6.0/MorganStanley.ComposeUI.TestConsoleApp2.exe");
    }
}
