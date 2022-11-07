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
    internal class ComposeHostedWebApp : IModuleRunner
    {
        public event EventHandler? StoppedUnexpectedly;
        private Process _process;
        private readonly string _path;
        private readonly int _port;

        public ComposeHostedWebApp(string path, int port)
        {
            _path = path;
            _port = port;
        }

        public Task<int> Launch()
        {
            var webserverPath = Path.GetFullPath("Runners\\webserver.cmd");

            _process = new Process();
            _process.StartInfo.UseShellExecute = true;
            _process.StartInfo.FileName = webserverPath;
            _process.StartInfo.ArgumentList.Add(_port.ToString());
            _process.StartInfo.WorkingDirectory = Path.GetFullPath(_path);
            _process.Start();

            return Task.Factory.StartNew( () =>
            {
                Task.Delay(TimeSpan.FromMilliseconds(500));
                return _process.Id;
            });
        }

        public async Task Stop()
        {
            _process.CloseMainWindow();
            var forceExit = 10;
            while (!_process.HasExited && forceExit > 0)
            {
                await Task.Delay(TimeSpan.FromMilliseconds(100));
                forceExit--;
            }
            if (forceExit == 0)
            {
                _process.Kill();
            }
        }
    }
}
