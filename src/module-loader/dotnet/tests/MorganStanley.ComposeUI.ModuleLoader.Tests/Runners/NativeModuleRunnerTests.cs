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

using System.Security;
using Moq;
using MorganStanley.ComposeUI.ModuleLoader.Runners;

namespace MorganStanley.ComposeUI.ModuleLoader.Tests.Runners;

public class NativeModuleRunnerTests
{
    private readonly NativeModuleRunner _runner = new NativeModuleRunner();

    [Fact]
    public void ModuleType_is_Native()
    {
        Assert.Equal(ModuleType.Native, _runner.ModuleType);
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
            Path = new Uri(Path.GetFullPath(@"..\..\..\..\NativeRunnerTestApp\bin\Debug\net6.0\NativeRunnerTestApp.exe")),
#else
            Path = new Uri(Path.GetFullPath(@"..\..\..\..\NativeRunnerTestApp\bin\Release\net6.0\NativeRunnerTestApp.exe")),
#endif

            Arguments = new[] { "Hello", "ComposeUI!", "I am", randomString }
        };
        manifest.SetupGet(x => x.Details).Returns(details);
        var moduleInstance = new ModuleInstance(Guid.NewGuid(), manifest.Object, startRequest);
        var startupContext = new StartupContext(startRequest, moduleInstance);

        await _runner.Start(startupContext, () => RedirectMainProcessOutput(startupContext));

        var processInfo = startupContext.GetOrAddProperty<MainProcessInfo>(() =>
        {
            Assert.Fail("No MainProcessInfo found");
            return null;
        });

        var mainProcess = processInfo.MainProcess;
        await mainProcess.WaitForExitAsync(new CancellationTokenSource(1000).Token);
        var stdout = mainProcess.StandardOutput.ReadToEnd().TrimEnd();
        Assert.Equal($"Hello ComposeUI! I am {randomString}", stdout);
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
            Path = new Uri(Path.GetFullPath(@"..\..\..\..\NativeRunnerTestApp\bin\Debug\net6.0\NativeRunnerTestApp.exe")),
#else
            Path = new Uri(Path.GetFullPath(@"..\..\..\..\NativeRunnerTestApp\bin\Release\net6.0\NativeRunnerTestApp.exe")),
#endif
            EnvironmentVariables = new Dictionary<string, string> { { variableName, randomString } }
        };
        manifest.SetupGet(x => x.Details).Returns(details);
        var moduleInstance = new ModuleInstance(Guid.NewGuid(), manifest.Object, startRequest);
        var startupContext = new StartupContext(startRequest, moduleInstance);

        await _runner.Start(startupContext, () => RedirectMainProcessOutput(startupContext));

        var processInfo = startupContext.GetOrAddProperty<MainProcessInfo>(() =>
        {
            Assert.Fail("No MainProcessInfo found");
            return null;
        });

        var mainProcess = processInfo.MainProcess;
        await mainProcess.WaitForExitAsync(new CancellationTokenSource(1000).Token);
        var stdout = mainProcess.StandardOutput.ReadToEnd().Split();
        Assert.Contains($"{variableName}={randomString}", stdout);
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
            Path = new Uri(@"..\..\..\..\NativeRunnerTestApp\bin\Debug\net6.0\NativeRunnerTestApp.exe", UriKind.Relative),
#else
            Path = new Uri(@"..\..\..\..\NativeRunnerTestApp\bin\Release\net6.0\NativeRunnerTestApp.exe", UriKind.Relative),
#endif
            EnvironmentVariables = new Dictionary<string, string> { { variableName, randomString } }
        };
        manifest.SetupGet(x => x.Details).Returns(details);
        var moduleInstance = new ModuleInstance(Guid.NewGuid(), manifest.Object, startRequest);
        var startupContext = new StartupContext(startRequest, moduleInstance);

        await _runner.Start(startupContext, () => RedirectMainProcessOutput(startupContext));

        var processInfo = startupContext.GetOrAddProperty<MainProcessInfo>(() =>
        {
            Assert.Fail("No MainProcessInfo found");
            return null;
        });

        var mainProcess = processInfo.MainProcess;
        await mainProcess.WaitForExitAsync(new CancellationTokenSource(1000).Token);
        var stdout = mainProcess.StandardOutput.ReadToEnd().Split();
        Assert.Contains($"{variableName}={randomString}", stdout);
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
            Path = new Uri(Path.GetFullPath(@"..\..\..\..\NativeRunnerTestApp\bin\Debug\net6.0\NativeRunnerTestApp.exe")),
#else
            Path = new Uri(Path.GetFullPath(@"..\..\..\..\NativeRunnerTestApp\bin\Release\net6.0\NativeRunnerTestApp.exe")),
#endif
            EnvironmentVariables = new Dictionary<string, string> { { variableName, randomString } }
        };
        manifest.SetupGet(x => x.Details).Returns(details);
        var moduleInstance = new ModuleInstance(Guid.NewGuid(), manifest.Object, startRequest);
        var startupContext = new StartupContext(startRequest, moduleInstance);

        await _runner.Start(startupContext, () =>
        {
            startupContext.AddProperty(new EnvironmentVariables(new Dictionary<string, string> { { variableName, randomString } }));
            return RedirectMainProcessOutput(startupContext);
        });

        var processInfo = startupContext.GetOrAddProperty<MainProcessInfo>(() =>
        {
            Assert.Fail("No MainProcessInfo found");
            return null;
        });

        var mainProcess = processInfo.MainProcess;
        await mainProcess.WaitForExitAsync(new CancellationTokenSource(1000).Token);
        var stdout = mainProcess.StandardOutput.ReadToEnd().Split();
        Assert.Contains($"{variableName}={randomString}", stdout);
    }

    private Task RedirectMainProcessOutput(StartupContext startupContext)
    {
        var mainProcessInfo = startupContext.GetProperties<MainProcessInfo>().First();
        mainProcessInfo.MainProcess.StartInfo.RedirectStandardOutput = true;
        return Task.CompletedTask;
    }
}
