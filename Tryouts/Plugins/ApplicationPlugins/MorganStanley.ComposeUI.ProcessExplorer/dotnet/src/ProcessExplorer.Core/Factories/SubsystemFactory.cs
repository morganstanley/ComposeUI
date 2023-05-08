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

using Microsoft.Extensions.Logging;
using ProcessExplorer.Abstractions.Infrastructure;
using ProcessExplorer.Abstractions.Subsystems;
using ProcessExplorer.Core.Subsystems;

namespace ProcessExplorer.Core.Factories;

public static class SubsystemFactory
{
    public static ISubsystemController CreateSubsystemController(
        ISubsystemLauncherCommunicator subsystemCommunicator,
        IUiHandler uiHandler,
        ILogger? logger)
    {
        return new SubsystemController(subsystemCommunicator, uiHandler, logger);
    }

    public static ISubsystemController CreateSubsystemController(
        ISubsystemLauncher subsystemLauncher,
        IUiHandler uiHandler,
        ILogger? logger)
    {
        return new SubsystemController(subsystemLauncher, uiHandler, logger);
    }
}
