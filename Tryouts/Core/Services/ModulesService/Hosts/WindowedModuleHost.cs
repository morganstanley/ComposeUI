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

namespace MorganStanley.ComposeUI.Tryouts.Core.Services.ModulesService.Hosts
{
    internal class WindowedModuleHost : ModuleHostBase
    {
        private IWindowedModuleRunner _moduleRunner;
        private ProcessInfo _processInfo;

        public WindowedModuleHost(string name, Guid instanceId, IWindowedModuleRunner moduleRunner) : base(name, instanceId)
        {
            _moduleRunner = moduleRunner;
            _moduleRunner.StoppedUnexpectedly += HandleUnexpectedStop;
            _processInfo = new ProcessInfo(name: Name, instanceId: InstanceId,uiType: UIType.Window,uiHint: _moduleRunner.MainWindowHandle.ToString(),pid: 0);
        }

        public override ProcessInfo ProcessInfo => _processInfo;

        public override async Task Launch()
        {
            var pid = await _moduleRunner.Launch();
            while (_moduleRunner.MainWindowHandle.ToInt64() == 0)
            {
                await Task.Delay(TimeSpan.FromMilliseconds(100));
            }

            _processInfo.pid = pid;
            _lifecycleEvents.OnNext(LifecycleEvent.Started(ProcessInfo));
        }

        public override async Task Teardown()
        {
            await _moduleRunner.Stop();
            _lifecycleEvents.OnNext(LifecycleEvent.Stopped(ProcessInfo));
        }

        private void HandleUnexpectedStop(object? sender, EventArgs e)
        {
            _lifecycleEvents.OnNext(LifecycleEvent.Stopped(ProcessInfo, false));
        }
    }
}
