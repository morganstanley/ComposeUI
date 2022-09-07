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
using MorganStanley.ComposeUI.Tryouts.Core.BasicModels.Modules;
using MorganStanley.ComposeUI.Tryouts.Core.Utilities;

namespace MorganStanley.ComposeUI.Plugins.ViewModelPlugins.ModulesVisualService
{
    public class SingleProcessViewModel : ViewModelBase, ISingleProcessViewModel
    {
        public event Action<ISingleProcessViewModel>? StopEvent;

        public event Action<ISingleProcessViewModel>? StoppedEvent;

        public event Action<ISingleProcessViewModel>? StartedEvent;

        public Guid InstanceId { get; }

        public string ProcessName { get; }

        public string UiType { get; }

        public long ProcessMainWindowHandle { get; set; }

        private bool _isRunning = false;
        public bool IsRunning
        {
            get => _isRunning;
            private set
            {
                if (_isRunning == value)
                {
                    return;
                }

                _isRunning = value;

                OnPropertyChanged(nameof(IsRunning));
                OnPropertyChanged(nameof(Status));
                OnPropertyChanged(nameof(CanStop));
            }
        }

        public string Status => IsRunning ? "Running" : "Stopped";

        public SingleProcessViewModel
        (
            Guid instanceId,
            string name, 
            string uiType
        ) 
        {
            InstanceId = instanceId;
            ProcessName = name;
            UiType = uiType;
        }

        public void ReactToMessage(LifecycleEvent lifecycleEvent)
        {
            if (lifecycleEvent.EventType == LifecycleEventType.StoppingCanceled || 
                lifecycleEvent.EventType == LifecycleEventType.FailedToStart)
            {
                return; // no change whatever it is
            }

            bool isStartedEvent = lifecycleEvent.EventType == LifecycleEventType.Started;

            IsRunning = isStartedEvent;

            if (isStartedEvent)
            {
                this.ProcessMainWindowHandle = long.Parse(lifecycleEvent.ProcessInfo.uiHint!);

                StartedEvent?.Invoke(this);

                OnPropertyChanged(nameof(ProcessMainWindowHandle));
            }
            else
            {
                StoppedEvent?.Invoke(this);
            }
        }

        public void Stop()
        {
            StopEvent?.Invoke(this);
        }

        public bool CanStop => IsRunning;
    }
}
