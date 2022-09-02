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

using MorganStanley.ComposeUI.Tryouts.Core.BasicModels.Modules;
using NP.Avalonia.UniDockService;
using NP.Concepts.Behaviors;
using System;
using System.Reactive.Linq;

namespace MorganStanley.ComposeUI.Plugins.ViewModelPlugins.ModulesVisualService
{
    public class ProcessDockLayoutBehavior : IProcessDockLayoutBehavior
    {
        IUniDockService _uniDockService;
        IProcessesViewModel _processesViewModel;
        string _dockSerializationFileName;
        string _vmSerializationFileName;
        string _mainGroupDockId;
        string _contentTemplateResourceKey;
        string _headerContentTemplateResourceKey;

        int _newDockId = 0;

        SynchronizationContext _synchronizationContext;

        IDisposable? _uniDockVmBehavior;
        IDisposable? _processesWithWindowsBehavior;

        public ProcessDockLayoutBehavior
        (
            IUniDockService uniDockService,
            IProcessesViewModel processesViewModel,
            string dockSerializationFileName,
            string vmSerializationFileName,
            string mainGroupDockId,
            string contentTemplateResourceKey,
            string headerContentTemplateResourceKey
        )
        {
            _uniDockService = uniDockService;
            _processesViewModel = processesViewModel;
            _dockSerializationFileName = dockSerializationFileName;
            _vmSerializationFileName = vmSerializationFileName;
            _mainGroupDockId = mainGroupDockId;
            _contentTemplateResourceKey = contentTemplateResourceKey;
            _headerContentTemplateResourceKey = headerContentTemplateResourceKey;

#pragma warning disable CS8974 // Converting method group to non-delegate type
            _processesWithWindowsBehavior = 
                _processesViewModel.ProcessesWithWindows.AddBehavior(OnProcessWithWindowAdded, OnProcessWithWindowRemoved);
#pragma warning restore CS8974 // Converting method group to non-delegate type

            _synchronizationContext = SynchronizationContext.Current!;

            _uniDockService.DockItemsViewModels = new System.Collections.ObjectModel.ObservableCollection<DockItemViewModelBase>();

            ResetBehavior();
        }

        private void ResetBehavior()
        {
            _uniDockVmBehavior?.Dispose();

            _uniDockVmBehavior = _uniDockService.DockItemsViewModels.AddBehavior(OnVmAdded, OnVmRemoved);
        }

        private void OnVmAdded(DockItemViewModelBase vm)
        {

        }

        private void OnVmRemoved(DockItemViewModelBase vm)
        {
            if (_isLoading)
            {
                return;
            }
            DockItemViewModel<ProcessData> processVm = 
                (DockItemViewModel<ProcessData>)vm;

            Guid instanceId = 
                processVm.TheVM!.InstanceId;

            _processesViewModel.StopProcess(instanceId);
        }

        public void Save()
        {
            _uniDockService.SaveToFile(_dockSerializationFileName);

            _uniDockService.SaveViewModelsToFile(_vmSerializationFileName);
        }

        bool _isLoading = false;
        public void Load()
        {
            _isLoading = true;

            try
            {
                _uniDockVmBehavior?.Dispose();
                _uniDockVmBehavior = null;
                _processesWithWindowsBehavior?.Dispose();
                _processesWithWindowsBehavior = null;
                _uniDockService.RestoreFromFile(_dockSerializationFileName, false);
                _uniDockService.RestoreViewModelsFromFile(_vmSerializationFileName, typeof(DockItemViewModel<ProcessData>));

                _newDockId =
                    _uniDockService
                        .DockItemsViewModels
                        .Cast<DockItemViewModel<ProcessData>>()
                        .Max(dockItemVm => dockItemVm.TheVM!.WindowNumber);

                var allInstanceIds =
                    _uniDockService
                        .DockItemsViewModels
                        .Cast<DockItemViewModel<ProcessData>>()
                        .Select(vm => vm.TheVM.InstanceId)
                        .Distinct()
                        .ToList();

                _processesViewModel.ClearProcessesThatDoNotMatchIds(allInstanceIds);


                _processesWithWindowsBehavior =
                    _processesViewModel
                        .ProcessesWithWindows
                            .AddBehavior(OnProcessWithWindowAdded, OnProcessWithWindowRemoved);

                foreach (DockItemViewModel<ProcessData> vm in _uniDockService.DockItemsViewModels)
                {
                    string processName = vm.TheVM!.ProcessName!;

                    Guid instanceId = vm.TheVM.InstanceId;

                    bool launchedProcess = _processesViewModel!.LaunchProcess(processName, instanceId);

                    if (!launchedProcess)
                    {
                        ISingleProcessViewModel? singleProcessViewModel = _processesViewModel?.FindRunningProcess(instanceId);

                        if (singleProcessViewModel != null)
                        {
                            OnProcessWithWindowAdded(singleProcessViewModel);
                        }
                    }
                }
                ResetBehavior();
            }
            finally
            {
                _isLoading = false;
            }
        }


        private void OnProcessWithWindowAdded(ISingleProcessViewModel processViewModel)
        {
            _synchronizationContext.Send
            (
                (_) =>
                {
                    Guid instanceId = processViewModel.InstanceId;

                    var existingVm =
                        _uniDockService
                            .DockItemsViewModels
                            .Cast<DockItemViewModel<ProcessData>>()
                            .FirstOrDefault(dockItemVm => dockItemVm.TheVM!.InstanceId == instanceId);

                    if (existingVm != null)
                    {
                        existingVm.TheVM!.ProcessMainWindowHandle = processViewModel.ProcessMainWindowHandle;
                    }
                    else
                    {
                        _newDockId++;
                        var dockItemViewModel = new DockItemViewModel<ProcessData>
                        {
                            DockId = _newDockId.ToString(),
                            DefaultDockGroupId = _mainGroupDockId,
                            DefaultDockOrderInGroup = _newDockId,
                            ContentTemplateResourceKey = _contentTemplateResourceKey,
                            HeaderContentTemplateResourceKey = _headerContentTemplateResourceKey,
                            IsSelected = true,
                            IsActive = true,
                            IsPredefined = false,
                        };

                        dockItemViewModel.TheVM = new ProcessData
                        {
                            InstanceId = processViewModel.InstanceId,
                            ProcessName = processViewModel.ProcessName,
                            WindowNumber = _newDockId,
                            ProcessMainWindowHandle = processViewModel.ProcessMainWindowHandle
                        };
                        _uniDockService!.DockItemsViewModels.Add(dockItemViewModel);
                    }
                },
                null
            );

        }

        private void OnProcessWithWindowRemoved(ISingleProcessViewModel processViewModel)
        {
            if (_isLoading)
            {
                return;
            }
            _synchronizationContext.Send((_) =>
            {
                var dockVm =
                    _uniDockService
                    .DockItemsViewModels
                    .Cast<DockItemViewModel<ProcessData>>()
                    .FirstOrDefault(vm => vm.TheVM!.InstanceId == processViewModel.InstanceId);

                if (dockVm != null)
                {
                    var dockId = dockVm.DockId;

                    var group = _uniDockService.GetGroupByDockId(dockId);

                    _uniDockService.DockItemsViewModels.Remove(dockVm);
                }
            },
            null);
        }
    }
}
