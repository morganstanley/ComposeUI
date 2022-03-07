/* Morgan Stanley makes this available to you under the Apache License, Version 2.0 (the "License"). You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0. See the NOTICE file distributed with this work for additional information regarding copyright ownership. Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the License for the specific language governing permissions and limitations under the License. */

using LocalCollector.Registrations;
using Microsoft.Extensions.DependencyInjection;

namespace ProcessExplorer.Entities.Registrations
{
    public class RegistrationMonitorDto
    {
        public SynchronizedCollection<RegistrationDto> Services { get; set; } = new SynchronizedCollection<RegistrationDto>();

        private static readonly object locker = new object();
        public static RegistrationMonitorDto FromCollection(ICollection<RegistrationDto> services)
        {
            var monitor = new RegistrationMonitorDto();
            lock (locker)
            {
                foreach (var item in services)
                {
                    monitor.Services?.Add(item);
                }
            }
            return monitor;
        }

        public static RegistrationMonitorDto FromCollection(IServiceCollection services)
        {
            var monitor = new RegistrationMonitorDto();
            lock (locker)
            {
                foreach (var item in services)
                {
                    var registraion = new RegistrationDto();
                    if (item is not null)
                    {
                        registraion.ServiceType = nameof(item.ServiceType);
                        registraion.ImplementationType = nameof(item.ImplementationType);
                        registraion.LifeTime = nameof(item.Lifetime);
                        monitor.Services?.Add(registraion);
                    }
                }
            }
            return monitor;
        }
    }
}
