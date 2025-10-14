/* 
 *  Morgan Stanley makes this available to you under the Apache License,
 *  Version 2.0 (the "License"). You may obtain a copy of the License at
 *       http://www.apache.org/licenses/LICENSE-2.0.
 *  See the NOTICE file distributed with this work for additional information
 *  regarding copyright ownership. Unless required by applicable law or agreed
 *  to in writing, software distributed under the License is distributed on an
 *  "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express
 *  or implied. See the License for the specific language governing permissions
 *  and limitations under the License.
 */

using System.Text.Json.Serialization;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.Shared.Converters;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.Shared.Protocol;

namespace MorganStanley.ComposeUI.Fdc3.DesktopAgent.Shared.Contracts;

internal sealed class RaiseIntentForContextRequest
{
    /// <summary>
    /// An identifier for the message.
    /// </summary>
    public int MessageId { get; set; }

    /// <summary>
    /// InstanceId of the application that sent the request.
    /// </summary>
    public string Fdc3InstanceId { get; set; }

    /// <summary>
    /// Context for identifying more the specific app that should handle the raised intent.
    /// </summary>
    [JsonConverter(typeof(ContextJsonConverter))]
    public string Context { get; set; }

    /// <summary>
    /// Information about the app that should resolve the raised intent.
    /// </summary>
    public AppIdentifier TargetAppIdentifier { get; set; }
}