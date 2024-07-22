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

using MorganStanley.ComposeUI.Fdc3.DesktopAgent.Contracts;

namespace MorganStanley.ComposeUI.Fdc3.DesktopAgent.Infrastructure.Internal;

internal partial class RaiseIntentResolutionMessage
{
    /// <summary>
    /// Intent for identifying the endpoint.
    /// </summary>
    public string Intent { get; init; }

    /// <summary>
    /// InstanceId to identify the target application.
    /// </summary>
    public string TargetModuleInstanceId { get; init; }

    /// <summary>
    /// Request that should be sent to the target module.
    /// </summary>
    public RaiseIntentResolutionRequest Request { get; init; }
}
