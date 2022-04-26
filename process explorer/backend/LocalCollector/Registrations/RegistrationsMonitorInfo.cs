/* Morgan Stanley makes this available to you under the Apache License, Version 2.0 (the "License"). You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0. See the NOTICE file distributed with this work for additional information regarding copyright ownership. Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the License for the specific language governing permissions and limitations under the License. */

using Microsoft.Extensions.DependencyInjection;

namespace ProcessExplorer.LocalCollector.Registrations
{
    public class RegistrationMonitorInfo
    {
        public SynchronizedCollection<RegistrationInfo> Services { get; internal set; } = new SynchronizedCollection<RegistrationInfo>();

        public static RegistrationMonitorInfo FromCollection(ICollection<RegistrationInfo> services)
        {
            var monitor = new RegistrationMonitorInfo();

            foreach (var item in services)
            {
                monitor.Services?.Add(item);
            }

            return monitor;
        }

        public static RegistrationMonitorInfo FromCollection(IServiceCollection services)
        {
            var monitor = new RegistrationMonitorInfo();

            foreach (var item in services)
            {
                var registration = new RegistrationInfo();
                registration.ServiceType = nameof(item.ServiceType);
                registration.ImplementationType = nameof(item.ImplementationType);
                registration.LifeTime = nameof(item.Lifetime);
                monitor.Services?.Add(registration);
            }

            return monitor;
        }
    }
}
