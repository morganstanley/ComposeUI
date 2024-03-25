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

namespace MorganStanley.ComposeUI.ModuleLoader.Runners;

internal class NativeModuleRunner : IModuleRunner
{
    public string ModuleType => ComposeUI.ModuleLoader.ModuleType.Native;

    public async Task Start(StartupContext startupContext, Func<Task> pipeline)
    {
        if (!startupContext.ModuleInstance.Manifest.TryGetDetails(out NativeManifestDetails details))
        {
            throw new Exception("Unable to get native manifest details");
        }

        startupContext.AddProperty(new EnvironmentVariables(details.EnvironmentVariables));


        var mainProcess = new Process();
        var processInfo = new MainProcessInfo(mainProcess);

        var filename = details.Path.IsAbsoluteUri ? details.Path.AbsolutePath : details.Path.ToString();

        mainProcess.StartInfo.FileName = Path.GetFullPath(filename);
        startupContext.AddProperty(processInfo);

        await pipeline();

        foreach (var argument in details.Arguments)
        {
            mainProcess.StartInfo.ArgumentList.Add(argument);
        }

        foreach (var envVar in startupContext.GetProperties<EnvironmentVariables>().SelectMany(x => x.Variables))
        {
            // TODO: what to do with duplicate envvars?
            if (!mainProcess.StartInfo.EnvironmentVariables.ContainsKey(envVar.Key))
            {
                mainProcess.StartInfo.EnvironmentVariables.Add(envVar.Key, envVar.Value);
            }
        }

        var unexpectedStopHandlers = startupContext.GetProperties<UnexpectedStopCallback>();

        foreach (var handler in unexpectedStopHandlers)
        {
            mainProcess.Exited += handler.ProcessStoppedUnexpectedly;
        }

        mainProcess.Start();

        
    }

    public async Task Stop(IModuleInstance moduleInstance)
    {
        MainProcessInfo? mainProcessInfo;

        if ((mainProcessInfo = (MainProcessInfo?) moduleInstance.GetProperties().FirstOrDefault(x => x is MainProcessInfo)) == null) { return; }

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
