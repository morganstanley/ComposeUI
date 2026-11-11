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
using FluentAssertions;
using FluentAssertions.Execution;
using Moq;
using MorganStanley.ComposeUI.ModuleLoader.Runners;
using MorganStanley.ComposeUI.ModuleLoader.Tests.TestUtils;

namespace MorganStanley.ComposeUI.ModuleLoader.Tests.Runners;

public class NativeModuleRunnerTests : IDisposable
{
    private readonly NativeModuleLaunchOptions _options = new();
    private readonly NativeModuleRunner _runner;
    private static readonly TimeSpan Timeout = TimeSpan.FromMinutes(1);
    private Process? _mainProcess;
    private string? _tempDirectory;

    public NativeModuleRunnerTests()
    {
        // The root of the test projects: the test app is started from there.
        _options.AllowedRoots.Add(Path.GetFullPath(@"..\..\..\.."));
        _runner = new NativeModuleRunner(_options);
    }

    [Fact]
    public void ModuleType_is_Native()
    {
        _runner.ModuleType.Should().Be(ModuleType.Native);
    }

    [Fact]
    public async Task ModuleIsLaunchedWithProvidedArguments()
    {
        var startRequest = new StartRequest("testModule");
        var manifest = new Mock<IModuleManifest<NativeManifestDetails>>();
        var randomString = Guid.NewGuid().ToString();
        var details = new NativeManifestDetails()
        {
#if DEBUG
            Path = new Uri(Path.GetFullPath(@"..\..\..\..\NativeRunnerTestApp\bin\Debug\net10.0\NativeRunnerTestApp.exe")),
#else
            Path = new Uri(Path.GetFullPath(@"..\..\..\..\NativeRunnerTestApp\bin\Release\net10.0\NativeRunnerTestApp.exe")),
#endif

            Arguments = new[] { "Hello", "ComposeUI!", "I am", randomString }
        };
        manifest.SetupGet(x => x.Details).Returns(details);
        var moduleInstance = new ModuleInstance(Guid.NewGuid(), manifest.Object, startRequest);
        var startupContext = new StartupContext(startRequest, moduleInstance);

        await _runner.Start(startupContext, () => RedirectMainProcessOutput(startupContext));

        var processInfo = startupContext.GetOrAddProperty<MainProcessInfo>(() =>
        {
            Execute.Assertion.FailWith("No MainProcessInfo found");
            return null!;
        });

        _mainProcess = processInfo.MainProcess;
        var result = await _mainProcess.WaitForExitAsync(Timeout);
        result.Output.Trim().Should().Be($"Hello ComposeUI! I am {randomString}");
    }

    [Fact]
    public async Task AbsolutePathModuleIsLaunchedWithEnvironmentVariablesFromManifest()
    {
        var startRequest = new StartRequest("testModule");
        var manifest = new Mock<IModuleManifest<NativeManifestDetails>>();
        const string variableName = "COMPOSEUI_TEST";
        var randomString = Guid.NewGuid().ToString();
        var details = new NativeManifestDetails()
        {
#if DEBUG
            Path = new Uri(Path.GetFullPath(@"..\..\..\..\NativeRunnerTestApp\bin\Debug\net10.0\NativeRunnerTestApp.exe")),
#else
            Path = new Uri(Path.GetFullPath(@"..\..\..\..\NativeRunnerTestApp\bin\Release\net10.0\NativeRunnerTestApp.exe")),
#endif
            EnvironmentVariables = new Dictionary<string, string> { { variableName, randomString } }
        };
        manifest.SetupGet(x => x.Details).Returns(details);
        var moduleInstance = new ModuleInstance(Guid.NewGuid(), manifest.Object, startRequest);
        var startupContext = new StartupContext(startRequest, moduleInstance);

        await _runner.Start(startupContext, () => RedirectMainProcessOutput(startupContext));

        var processInfo = startupContext.GetOrAddProperty<MainProcessInfo>(() =>
        {
            Execute.Assertion.FailWith("No MainProcessInfo found");
            return null!;
        });

        _mainProcess = processInfo.MainProcess;
        var result = await _mainProcess.WaitForExitAsync(Timeout);
        result.Output.Should().Contain($"{variableName}={randomString}");
    }

    [Fact]
    public async Task RelativePathModuleIsLaunchedWithEnvironmentVariablesFromManifest()
    {
        var startRequest = new StartRequest("testModule");
        var manifest = new Mock<IModuleManifest<NativeManifestDetails>>();
        const string variableName = "COMPOSEUI_TEST";
        var randomString = Guid.NewGuid().ToString();
        var details = new NativeManifestDetails()
        {
#if DEBUG
            Path = new Uri(@"NativeRunnerTestApp\bin\Debug\net10.0\NativeRunnerTestApp.exe", UriKind.Relative),
#else
            Path = new Uri(@"NativeRunnerTestApp\bin\Release\net10.0\NativeRunnerTestApp.exe", UriKind.Relative),
#endif
            EnvironmentVariables = new Dictionary<string, string> { { variableName, randomString } }
        };
        manifest.SetupGet(x => x.Details).Returns(details);
        var moduleInstance = new ModuleInstance(Guid.NewGuid(), manifest.Object, startRequest);
        var startupContext = new StartupContext(startRequest, moduleInstance);

        await _runner.Start(startupContext, () => RedirectMainProcessOutput(startupContext));

        var processInfo = startupContext.GetOrAddProperty<MainProcessInfo>(() =>
        {
            Execute.Assertion.FailWith("No MainProcessInfo found");
            return null!;
        });

        _mainProcess = processInfo.MainProcess;
        var result = await _mainProcess.WaitForExitAsync(Timeout);
        result.Output.Should().Contain($"{variableName}={randomString}");
    }

    [Fact]
    public async Task ModuleIsLaunchedWithEnvironmentVariablesFromPipeline()
    {
        var startRequest = new StartRequest("testModule");
        var manifest = new Mock<IModuleManifest<NativeManifestDetails>>();
        const string variableName = "COMPOSEUI_TEST";
        var randomString = Guid.NewGuid().ToString();
        var details = new NativeManifestDetails()
        {
#if DEBUG
            Path = new Uri(Path.GetFullPath(@"..\..\..\..\NativeRunnerTestApp\bin\Debug\net10.0\NativeRunnerTestApp.exe")),
#else
            Path = new Uri(Path.GetFullPath(@"..\..\..\..\NativeRunnerTestApp\bin\Release\net10.0\NativeRunnerTestApp.exe")),
#endif
            EnvironmentVariables = new Dictionary<string, string> { { variableName, randomString } }
        };
        manifest.SetupGet(x => x.Details).Returns(details);
        var moduleInstance = new ModuleInstance(Guid.NewGuid(), manifest.Object, startRequest);
        var startupContext = new StartupContext(startRequest, moduleInstance);

        await _runner.Start(startupContext, () =>
        {
            return RedirectMainProcessOutput(startupContext);
        });

        var processInfo = startupContext.GetOrAddProperty<MainProcessInfo>(() =>
        {
            Execute.Assertion.FailWith("No MainProcessInfo found");
            return null!;
        });

        _mainProcess = processInfo.MainProcess;
        var result = await _mainProcess.WaitForExitAsync(Timeout);
        result.Output.Should().Contain($"{variableName}={randomString}");
    }

    [Fact]
    public async Task AbsolutePathModuleIsLaunchedFromADirectoryWithSpacesInItsName()
    {
#if DEBUG
        var sourceExe = Path.GetFullPath(@"..\..\..\..\NativeRunnerTestApp\bin\Debug\net10.0\NativeRunnerTestApp.exe");
#else
        var sourceExe = Path.GetFullPath(@"..\..\..\..\NativeRunnerTestApp\bin\Release\net10.0\NativeRunnerTestApp.exe");
#endif
        _tempDirectory = Path.Combine(Path.GetTempPath(), "composeui runner " + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDirectory);
        _options.AllowedRoots.Add(_tempDirectory);

        foreach (var file in Directory.GetFiles(Path.GetDirectoryName(sourceExe)!))
        {
            File.Copy(file, Path.Combine(_tempDirectory, Path.GetFileName(file)));
        }

        var startRequest = new StartRequest("testModule");
        var manifest = new Mock<IModuleManifest<NativeManifestDetails>>();
        var randomString = Guid.NewGuid().ToString();
        var details = new NativeManifestDetails()
        {
            Path = new Uri(Path.Combine(_tempDirectory, Path.GetFileName(sourceExe)), UriKind.Absolute),
            Arguments = new[] { "Hello", randomString }
        };
        manifest.SetupGet(x => x.Details).Returns(details);
        var moduleInstance = new ModuleInstance(Guid.NewGuid(), manifest.Object, startRequest);
        var startupContext = new StartupContext(startRequest, moduleInstance);

        await _runner.Start(startupContext, () => RedirectMainProcessOutput(startupContext));

        var processInfo = startupContext.GetOrAddProperty<MainProcessInfo>(() =>
        {
            Execute.Assertion.FailWith("No MainProcessInfo found");
            return null!;
        });

        _mainProcess = processInfo.MainProcess;
        var result = await _mainProcess.WaitForExitAsync(Timeout);
        result.Output.Trim().Should().Be($"Hello {randomString}");
    }

    [Fact]
    public async Task ModuleOutsideTheAllowedRootsIsNotStartedAndTheStartupPipelineDoesNotRun()
    {
        var startRequest = new StartRequest("testModule");
        var manifest = new Mock<IModuleManifest<NativeManifestDetails>>();
        manifest.SetupGet(x => x.Id).Returns("testModule");
        manifest.SetupGet(x => x.Details).Returns(new NativeManifestDetails
        {
            Path = new Uri(Path.Combine(Path.GetTempPath(), "outside", "app.exe"), UriKind.Absolute)
        });
        var moduleInstance = new ModuleInstance(Guid.NewGuid(), manifest.Object, startRequest);
        var startupContext = new StartupContext(startRequest, moduleInstance);
        var pipelineRan = false;

        var action = () => _runner.Start(startupContext, () =>
        {
            pipelineRan = true;
            return Task.CompletedTask;
        });

        await action.Should().ThrowAsync<ModuleLoaderException>().WithMessage("*testModule*");
        pipelineRan.Should().BeFalse();
        startupContext.GetProperties<MainProcessInfo>().Should().BeEmpty();
    }

    [Fact]
    public async Task ModuleWhoseDetailsChangeDuringTheStartupPipelineIsCheckedAgainAndNotStarted()
    {
        var startRequest = new StartRequest("testModule");
        var manifest = new Mock<IModuleManifest<NativeManifestDetails>>();
        manifest.SetupGet(x => x.Id).Returns("testModule");
        var details = new NativeManifestDetails
        {
#if DEBUG
            Path = new Uri(@"NativeRunnerTestApp\bin\Debug\net10.0\NativeRunnerTestApp.exe", UriKind.Relative),
#else
            Path = new Uri(@"NativeRunnerTestApp\bin\Release\net10.0\NativeRunnerTestApp.exe", UriKind.Relative),
#endif
        };
        manifest.SetupGet(x => x.Details).Returns(details);
        var moduleInstance = new ModuleInstance(Guid.NewGuid(), manifest.Object, startRequest);
        var startupContext = new StartupContext(startRequest, moduleInstance);

        var action = () => _runner.Start(startupContext, () =>
        {
            details.Path = new Uri(Path.Combine(Path.GetTempPath(), "outside", "app.exe"), UriKind.Absolute);
            return Task.CompletedTask;
        });

        await action.Should().ThrowAsync<ModuleLoaderException>().WithMessage("*testModule*");
        var process = startupContext.GetProperties<MainProcessInfo>().Single().MainProcess;
        var processId = () => process.Id;
        processId.Should().Throw<InvalidOperationException>("the process was never started");
    }

    [Fact]
    public async Task ModuleWithABlockedEnvironmentVariableIsNotStarted()
    {
        var startRequest = new StartRequest("testModule");
        var manifest = new Mock<IModuleManifest<NativeManifestDetails>>();
        manifest.SetupGet(x => x.Id).Returns("testModule");
        manifest.SetupGet(x => x.Details).Returns(new NativeManifestDetails
        {
#if DEBUG
            Path = new Uri(@"NativeRunnerTestApp\bin\Debug\net10.0\NativeRunnerTestApp.exe", UriKind.Relative),
#else
            Path = new Uri(@"NativeRunnerTestApp\bin\Release\net10.0\NativeRunnerTestApp.exe", UriKind.Relative),
#endif
            EnvironmentVariables = new Dictionary<string, string> { { "DOTNET_STARTUP_HOOKS", "evil.dll" } }
        });
        var moduleInstance = new ModuleInstance(Guid.NewGuid(), manifest.Object, startRequest);
        var startupContext = new StartupContext(startRequest, moduleInstance);

        var action = () => _runner.Start(startupContext, () => Task.CompletedTask);

        await action.Should().ThrowAsync<ModuleLoaderException>().WithMessage("*DOTNET_STARTUP_HOOKS*");
    }

    [Fact]
    public async Task ModuleOutsideTheAllowedRootsIsStartedWhenTheLaunchGuardIsDisabled()
    {
        _options.AllowedRoots.Clear();
        _options.AllowedRoots.Add(Path.Combine(Path.GetTempPath(), "unrelated-root"));
        _options.DisableLaunchGuard = true;
        var startRequest = new StartRequest("testModule");
        var manifest = new Mock<IModuleManifest<NativeManifestDetails>>();
        manifest.SetupGet(x => x.Id).Returns("testModule");
        manifest.SetupGet(x => x.Details).Returns(new NativeManifestDetails
        {
#if DEBUG
            Path = new Uri(Path.GetFullPath(@"..\..\..\..\NativeRunnerTestApp\bin\Debug\net10.0\NativeRunnerTestApp.exe")),
#else
            Path = new Uri(Path.GetFullPath(@"..\..\..\..\NativeRunnerTestApp\bin\Release\net10.0\NativeRunnerTestApp.exe")),
#endif
            Arguments = new[] { "Hello" }
        });
        var moduleInstance = new ModuleInstance(Guid.NewGuid(), manifest.Object, startRequest);
        var startupContext = new StartupContext(startRequest, moduleInstance);

        await _runner.Start(startupContext, () => RedirectMainProcessOutput(startupContext));

        _mainProcess = startupContext.GetProperties<MainProcessInfo>().Single().MainProcess;
        var result = await _mainProcess.WaitForExitAsync(Timeout);
        result.Output.Trim().Should().Be("Hello");
    }

    private static Task RedirectMainProcessOutput(StartupContext startupContext)
    {
        var mainProcessInfo = startupContext.GetProperties<MainProcessInfo>().First();
        mainProcessInfo.MainProcess.StartInfo.RedirectStandardOutput = true;
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        // If the test timed out, kill the test process.
        if (_mainProcess != null && !_mainProcess.HasExited)
        {
            _mainProcess.Kill();
            _mainProcess.WaitForExit();
        }

        if (_tempDirectory != null && Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, recursive: true);
        }
    }
}
