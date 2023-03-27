/// ********************************************************************************************************
///
/// Morgan Stanley makes this available to you under the Apache License, Version 2.0 (the "License").
/// You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0.
/// See the NOTICE file distributed with this work for additional information regarding copyright ownership.
/// Unless required by applicable law or agreed to in writing, software distributed under the License
/// is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
/// See the License for the specific language governing permissions and limitations under the License.
/// 
/// ********************************************************************************************************

using MorganStanley.ComposeUI.Tryouts.Core.Abstractions.Modules;
using System.Diagnostics;

namespace MorganStanley.ComposeUI.Tryouts.Core.Services.ModulesService.Runners
{
    internal class ExecutableRunner : IWindowedModuleRunner
    {
        private Process? _mainProcess;
        private readonly string _launchPath;
        private readonly string[] _arguments;

        public IntPtr MainWindowHandle => _mainProcess?.MainWindowHandle ?? IntPtr.Zero;

        public event EventHandler? StoppedUnexpectedly;

        public ExecutableRunner(string launchPath, string[]? arguments)
        {
            _launchPath = launchPath;
            _arguments = arguments ?? Array.Empty<string>();
        }

        public Task<int> Launch()
        {
            var mainProcess = new Process();
            mainProcess.StartInfo.FileName = _launchPath;
            mainProcess.EnableRaisingEvents = true;
            mainProcess.StartInfo.UseShellExecute = true;
            mainProcess.Exited -= ProcessExitedUnexpectedly;
            mainProcess.Exited += ProcessExitedUnexpectedly;

            foreach (var argument in _arguments)
            {
                mainProcess.StartInfo.ArgumentList.Add(argument);
            }

            _mainProcess = mainProcess;

            _mainProcess?.Start();

            return Task.FromResult(mainProcess.Id);
        }

        public async Task Stop()
        {
            if (_mainProcess == null)
            {
                return;
            }

            _mainProcess.Exited -= ProcessExitedUnexpectedly;
            var killNecessary = true;

            if (_mainProcess.CloseMainWindow())
            {
                await Task.Delay(TimeSpan.FromMilliseconds(500));
                if (_mainProcess.HasExited)
                {
                    killNecessary = false;
                }
            }

            if (killNecessary)
            {
                _mainProcess.Kill();
                await Task.Delay(TimeSpan.FromMilliseconds(500));
            }
        }

        private void ProcessExitedUnexpectedly(object? sender, EventArgs e)
        {
            StoppedUnexpectedly?.Invoke(sender, e);
        }
    }
}
