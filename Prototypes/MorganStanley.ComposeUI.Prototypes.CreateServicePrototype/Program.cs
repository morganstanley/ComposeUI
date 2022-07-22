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

using MorganStanley.ComposeUI.Plugins.TimeService;
using MorganStanley.ComposeUI.Plugins.TimeViewModel;
using System;

namespace MorganStanley.ComposeUI.Prototypes.CreateServicePrototype
{
    internal class Program
    {
        public static void Main(string[] args)
        {
            IObservable<DateTime> timeService = TimeServicesFactory.CreateTimeService();

            TimeConsumerViewModel timeConsumerViewModel = new TimeConsumerViewModel(timeService);

            timeConsumerViewModel.PropertyChanged += (sender, e) =>
            {
                Console.WriteLine(timeConsumerViewModel.TheTime);
            };

            Console.ReadLine();
        }
    }
}
