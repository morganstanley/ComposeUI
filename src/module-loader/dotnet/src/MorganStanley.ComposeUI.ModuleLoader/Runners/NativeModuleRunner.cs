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
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace MorganStanley.ComposeUI.ModuleLoader.Runners;

internal class NativeModuleRunner : IModuleRunner
{
    private readonly NativeModuleLaunchOptions _options;
    private readonly ILogger<NativeModuleRunner> _logger;

    public NativeModuleRunner(NativeModuleLaunchOptions? options = null, ILogger<NativeModuleRunner>? logger = null)
    {
        _options = options ?? new NativeModuleLaunchOptions();
        _logger = logger ?? NullLogger<NativeModuleRunner>.Instance;
    }

    public string ModuleType => ComposeUI.ModuleLoader.ModuleType.Native;

    public async Task Start(StartupContext startupContext, Func<Task> pipeline)
    {
        if (!startupContext.ModuleInstance.Manifest.TryGetDetails(out NativeManifestDetails details))
        {
            throw new Exception("Unable to get native manifest details");
        }

        var fileName = GetFileName(startupContext.ModuleInstance.Manifest.Id, details);

        startupContext.AddProperty(new EnvironmentVariables(details.EnvironmentVariables));

        var properties = new NativeStartupProperties
        {
            InitialModulePosition = details.InitialModulePosition,
            Width = details.Width,
            Height = details.Height,
            Coordinates = details.Coordinates,
        };

        startupContext.AddProperty(properties);

        var mainProcess = new Process();
        var processInfo = new MainProcessInfo(mainProcess);

        mainProcess.StartInfo.FileName = fileName;
        startupContext.AddProperty(processInfo);

        //Waits for other IStartActions to finish first.
        await pipeline();

        // The startup actions run for a while, so check again right before the process is started.
        if (!_options.DisableLaunchGuard)
        {
            mainProcess.StartInfo.FileName = NativeLaunchGuard.Validate(startupContext.ModuleInstance.Manifest.Id, details, _options);
        }

        foreach (var argument in details.Arguments)
        {
            mainProcess.StartInfo.ArgumentList.Add(argument);
        }

        foreach (var envVar in startupContext.GetProperties<EnvironmentVariables>().SelectMany(x => x.Variables))
        {
            // TODO: what to do with duplicate envvars?
            if (mainProcess.StartInfo.EnvironmentVariables.ContainsKey(envVar.Key))
            {
                mainProcess.StartInfo.EnvironmentVariables.Remove(envVar.Key);
            }

            mainProcess.StartInfo.EnvironmentVariables.Add(envVar.Key, envVar.Value);
        }

        var unexpectedStopHandlers = startupContext.GetProperties<UnexpectedStopCallback>();

        foreach (var handler in unexpectedStopHandlers)
        {
            mainProcess.Exited += handler.ProcessStoppedUnexpectedly;
        }

        mainProcess.Start();
    }

    private string GetFileName(string moduleId, NativeManifestDetails details)
    {
        if (!_options.DisableLaunchGuard)
        {
            return NativeLaunchGuard.Validate(moduleId, details, _options);
        }

        _logger.LogWarning(
            "The launch guard is disabled: the native module '{ModuleId}' is started without checking its path, arguments and environment variables.",
            moduleId);

        return Path.GetFullPath(details.Path.IsAbsoluteUri ? details.Path.LocalPath : details.Path.ToString());
    }

    public async Task Stop(IModuleInstance moduleInstance)
    {
        MainProcessInfo? mainProcessInfo;

        if ((mainProcessInfo =
                (MainProcessInfo?) moduleInstance.GetProperties().FirstOrDefault(x => x is MainProcessInfo)) == null)
        {
            return;
        }

        var mainProcess = mainProcessInfo.MainProcess;

        // Detach unexpected stop callbacks
        foreach (var handler in moduleInstance.GetProperties<UnexpectedStopCallback>())
        {
            mainProcess.Exited -= handler.ProcessStoppedUnexpectedly;
        }

        var killNecessary = true;

        if (mainProcess.CloseMainWindow())
        {
            await Task.Delay(TimeSpan.FromMilliseconds(500));
            if (mainProcess.HasExited)
            {
                killNecessary = false;
            }
        }

        if (killNecessary)
        {
            mainProcess.Kill();
            await Task.Delay(TimeSpan.FromMilliseconds(500));
        }
    }
}
