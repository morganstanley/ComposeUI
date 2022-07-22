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

using MorganStanley.ComposeUI.Playground.Interfaces;
using NP.Utilities;
using NP.Utilities.Attributes;
using NP.Utilities.PluginUtils;
using System.Reactive.Concurrency;
using System.Reactive.Linq;

namespace MorganStanley.ComposeUI.Plugins.TimeViewModel
{

    [Implements(typeof(IPlugin), partKey:"TimeConsumerVM", isSingleton:true)]
    public class TimeConsumerViewModel : VMBase, IPlugin
    {
        private IDisposable _subscription;

        [CompositeConstructor]
        public TimeConsumerViewModel([Part(partKey:IoCNames.TimeServiceName)] IObservable<DateTime> timeService)
        {
            _subscription = 
                timeService
                    .ObserveOn(Scheduler.TaskPool)
                    .Subscribe(time => { TheTime = time; });
        }


        #region TheTime Property
        private DateTime _time = DateTime.Now;
        public DateTime TheTime
        {
            get
            {
                return this._time;
            }
            set
            {
                if (this._time == value)
                {
                    return;
                }

                this._time = value;
                this.OnPropertyChanged(nameof(TheTime));
            }
        }
        #endregion TheTime Property
    }
}
