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

using MorganStanley.ComposeUI.ModuleLoader;

namespace MorganStanley.ComposeUI.Fdc3.DesktopAgent.Infrastructure.Internal;

/// <summary>
/// Abstract base class for handling startup actions for different module types.
/// </summary>
internal abstract class StartupModuleHandler
{
    /// <summary>
    /// Executes the startup logic for the specified module type.
    /// </summary>
    /// <param name="startupContext">The startup context for the module.</param>
    /// <param name="appId">The application identifier.</param>
    /// <param name="fdc3InstanceId">The FDC3 instance identifier.</param>
    /// <param name="channelId">The channel identifier, if any.</param>
    /// <param name="openedAppContextId">The opened app context identifier, if any.</param>
    public abstract Task HandleAsync(StartupContext startupContext, string appId, string fdc3InstanceId, string? channelId, string? openedAppContextId);
}
