// Morgan Stanley makes this available to you under the Apache License,
// Version 2.0 (the "License"). You may obtain a copy of the License at
// 
//      http://www.apache.org/licenses/LICENSE-2.0.
// 
// See the NOTICE file distributed with this work for additional information
// regarding copyright ownership. Unless required by applicable law or agreed
// to in writing, software distributed under the License is distributed on an
// "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express
// or implied. See the License for the specific language governing permissions
// and limitations under the License.


using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ModuleProcessMonitor.Processes;
using MorganStanley.ComposeUI.Tryouts.Core.Abstractions.Modules;

namespace ModulesPrototype;

public interface IProcessInfoHandler
{
    ValueTask SendInitProcessInfoAsync(IEnumerable<ProcessInformation> processInfo);
    void SendAddProcessInfo(ProcessInfoData processInfo);
    ValueTask EnableProcessMonitorAsync();
    ValueTask DisableProcessMonitorAsync();
    ValueTask SendModifiedSubsystemStateAsync(Guid instanceId, string state);
    ValueTask InitializeSubsystemControllerRouteAsync();
    ValueTask SendRegisteredSubsystemsAsync(string subsystems);
    void SetSubsystemHandler(IModuleLoader moduleLoader, ILoggerFactory? loggerFactory = null);
}
