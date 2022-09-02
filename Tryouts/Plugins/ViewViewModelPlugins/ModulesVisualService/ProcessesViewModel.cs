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
using System.Collections.ObjectModel;
using System.Reactive.Linq;
using NP.Concepts.Behaviors;
using NP.Utilities;
using MorganStanley.ComposeUI.Tryouts.Core.BasicModels.Modules;

namespace MorganStanley.ComposeUI.Plugins.ViewModelPlugins.ModulesVisualService
{
    public class ProcessesViewModel : IProcessesViewModel
    {
        public ModuleViewModel[] Modules { get; }

        IModuleLoader _moduleLoader;


        private ObservableCollection<ISingleProcessViewModel> _processes = 
            new ObservableCollection<ISingleProcessViewModel>();
        public IEnumerable<ISingleProcessViewModel> Processes => _processes;


       private ObservableCollection<ISingleProcessViewModel> _processesWithWindows = 
            new ObservableCollection<ISingleProcessViewModel>();
        public IEnumerable<ISingleProcessViewModel> ProcessesWithWindows => _processesWithWindows;

        private IDisposable _modulesBehavior;
        private IDisposable _processesBehavior;
        IModuleLoaderFactory _moduleLoaderFactory;
        public ProcessesViewModel
        (
            IModuleLoaderFactory moduleLoaderFactory,
            ModuleViewModel[] modules)
        {
            _moduleLoaderFactory = moduleLoaderFactory;

            Modules = modules;

            _modulesBehavior = Modules.AddBehavior(OnModuleAdded, OnModuleRemoved);

            _processesBehavior = _processes.AddBehavior(OnProcessesAdded, OnProcessesRemoved);

            ModuleCatalogue moduleCatalogue =
                new ModuleCatalogue(Modules.ToDictionary(m => m.Manifest.Name, m => m.Manifest));

            _moduleLoader = _moduleLoaderFactory.Create(moduleCatalogue);

            _moduleLoader.LifecycleEvents.Subscribe(OnLifecycleEventArrived);
        }

        internal void ResetProcesses()
        {
            (Processes as IList<SingleProcessViewModel>).RemoveAllOneByOne();
        }

        private void OnLifecycleEventArrived(LifecycleEvent lifecycleEvent)
        {
            Guid instanceId = lifecycleEvent.ProcessInfo.instanceId;

            var process = Processes.FirstOrDefault(p => p.InstanceId == instanceId);

            ((SingleProcessViewModel) process!)?.ReactToMessage(lifecycleEvent);
        }

        private void OnModuleAdded(ModuleViewModel module)
        {
            module.LaunchEvent += OnLaunchModule;
        }

        private void OnModuleRemoved(ModuleViewModel module)
        {
            module.LaunchEvent -= OnLaunchModule;
        }

        private void OnProcessesAdded(ISingleProcessViewModel process)
        {
            process.StopEvent += OnStopProcess;
            process.StartedEvent += Process_StartedEvent;
            process.StoppedEvent += Process_StoppedEvent;
        }

        private void Process_StoppedEvent(ISingleProcessViewModel process)
        {
            _processesWithWindows.Remove(process);
        }

        private void Process_StartedEvent(ISingleProcessViewModel process)
        {
            _processesWithWindows.Add(process);
        }

        private void OnProcessesRemoved(ISingleProcessViewModel process)
        {
            process.StoppedEvent -= Process_StoppedEvent;
            process.StartedEvent -= Process_StartedEvent;
            process.StopEvent -= OnStopProcess;
        }

        private void OnLaunchModule(ModuleManifest moduleInfo)
        {
            Guid instanceId = Guid.NewGuid();

            LaunchProcess(moduleInfo, instanceId);
        }

        public ModuleManifest? FindManifest(string processName)
        {
            return Modules?.FirstOrDefault(module => module.Manifest.Name == processName)?.Manifest;
        }


        public ISingleProcessViewModel? FindRunningProcess(Guid instanceId)
        {
            return ProcessesWithWindows.FirstOrDefault(p => p.InstanceId == instanceId);
        }

        /// <summary>
        /// returns true if launching aprocess, false, if it is an old process
        /// </summary>
        /// <param name="moduleInfo"></param>
        /// <param name="instanceId"></param>
        /// <returns></returns>
        public bool LaunchProcess(ModuleManifest moduleInfo, Guid instanceId)
        {
            ISingleProcessViewModel? processViewModel = FindRunningProcess(instanceId);

            bool hasProcessRunning = processViewModel != null;

            if (processViewModel == null)
            {
                processViewModel = 
                    new SingleProcessViewModel(instanceId, moduleInfo.Name, moduleInfo.UIType);
            }

            if (!_processes.Any(p => p.InstanceId == instanceId))
            {
                _processes.Add(processViewModel);
            }

            if (hasProcessRunning)
            {
                return false;
            }

            _moduleLoader.RequestStartProcess
            (
                new LaunchRequest
                {
                    name = moduleInfo.Name,
                    instanceId = instanceId
                });

            return true;
        }

        /// <summary>
        /// returns true if launching aprocess, false, if it is an old process
        /// </summary>
        /// <param name="moduleInfo"></param>
        /// <param name="instanceId"></param>
        /// <returns></returns>
        public bool LaunchProcess(string processName, Guid instanceId)
        {
            ModuleManifest? moduleManifest = FindManifest(processName);

            if (moduleManifest == null)
            {
                throw new Exception($"Module with Name '{processName}' is not found");
            }

            return LaunchProcess(moduleManifest, instanceId);
        }

        private void OnStopProcess(ISingleProcessViewModel process)
        {
            StopRequest stopRequest = new StopRequest();
            stopRequest.instanceId = process.InstanceId;
            _moduleLoader.RequestStopProcess(stopRequest);
        }

        public void StopProcess(Guid instanceId)
        {
            ISingleProcessViewModel? singleProcessViewModel = 
                Processes.FirstOrDefault(p => p.InstanceId == instanceId);

            OnStopProcess(singleProcessViewModel);
        }

        public void ClearProcessesThatDoNotMatchIds(IEnumerable<Guid> instanceIds)
        {
            foreach(ISingleProcessViewModel process in this.Processes.ToList())
            {
                if (!instanceIds.Any(id => id == process.InstanceId))
                {
                    _processes.Remove(process);
                    process.Stop();
                    _processesWithWindows.Remove(process);
                }
            }
        }
    }
}
