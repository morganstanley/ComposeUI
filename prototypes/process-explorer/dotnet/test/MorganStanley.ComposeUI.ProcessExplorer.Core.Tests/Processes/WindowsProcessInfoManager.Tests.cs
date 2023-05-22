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
using Microsoft.Extensions.Logging;
using Moq;
using MorganStanley.ComposeUI.ProcessExplorer.Abstractions.Processes;
using MorganStanley.ComposeUI.ProcessExplorer.Core.Processes;
using Xunit;

namespace MorganStanley.ComposeUI.ProcessExplorer.Core.Tests.Processes;

public class WindowsProcessInfoManagerTests
{
    [Fact]
    public void AddChildProcesses_will_add_child_processes_to_the_list()
    {
        var loggerMock = CreateLoggerMock();
        var processMonitor = CreateWindowsProcessMonitor(loggerMock.Object);
        var testApplication = Process.Start(GetTestApplicationPath()); //It will start an another child process

        // Waiting for the child process to start
        Thread.Sleep(100);

        processMonitor.AddChildProcesses(testApplication.Id, testApplication.ProcessName);

        var result = processMonitor.GetProcessIds().ToArray();

        Assert.NotEmpty(result);
        Assert.Equal(2, result.Length);
        Assert.Contains(testApplication.Id, result);

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

    [Fact] //It is hard to reproduce a process which ses cpu in a time that is being measured in percentage
    public void GetCpuUsage_will_return_with_some_value()
    {
        var loggerMock = CreateLoggerMock();
        var processMonitor = CreateWindowsProcessMonitor(loggerMock.Object);
        var process = Process.Start(GetSimpleTestApplicationPath());

        var cpuUsageResult = processMonitor.GetCpuUsage(
            process.Id, 
            process.ProcessName);

        Assert.True(cpuUsageResult >= 0);
        
        process.Kill();
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
    public void SetProcessIds_will_set_the_ids_and_its_child_process_ids()
    {
        var loggerMock = CreateLoggerMock();
        var processMonitor = CreateWindowsProcessMonitor(loggerMock.Object);

        var testApplication = Process.Start(GetTestApplicationPath()); //It will start a process

        var processes = new[] { testApplication.Id };

        // Waiting for the test process to start
        Thread.Sleep(1000);
        processMonitor.SetProcessIds(Environment.ProcessId, processes);

        var result = processMonitor.GetProcessIds().ToArray();

        Assert.NotStrictEqual(processes, result);
        Assert.Equal(3, result.Length);

        processMonitor.Dispose();
    }


    [Conditional("DEBUG")]
    private static void IsDebug(ref bool isDebug) => isDebug = true;

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
        var folder = GetEnvironmentFolder();

        return Path.GetFullPath($@"../../../../MorganStanley.ComposeUI.TestConsoleApp/bin/{folder}/net6.0/MorganStanley.ComposeUI.TestConsoleApp.exe");
    }

    private static string GetSimpleTestApplicationPath()
    {
        var folder = GetEnvironmentFolder();

        return Path.GetFullPath($"../../../../MorganStanley.ComposeUI.TestConsoleApp2/bin/{folder}/net6.0/MorganStanley.ComposeUI.TestConsoleApp2.exe");
    }

    private static string GetEnvironmentFolder()
    {
        var isDebug = false;
        IsDebug(ref isDebug);

        var folder = isDebug ? "Debug" : "Release";

        return folder;
    }
}
