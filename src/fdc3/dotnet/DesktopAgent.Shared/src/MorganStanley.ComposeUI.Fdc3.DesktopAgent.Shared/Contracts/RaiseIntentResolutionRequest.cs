/*
 * Morgan Stanley makes this available to you under the Apache License,
 * Version 2.0 (the "License"). You may obtain a copy of the License at
 *
 *      http://www.apache.org/licenses/LICENSE-2.0.
 *
 * See the NOTICE file distributed with this work for additional information
 * regarding copyright ownership. Unless required by applicable law or agreed
 * to in writing, software distributed under the License is distributed on an
 * "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express
 * or implied. See the License for the specific language governing permissions
 * and limitations under the License.
 */


using System.Text.Json.Serialization;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.Shared.Converters;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.Shared.Protocol;

namespace MorganStanley.ComposeUI.Fdc3.DesktopAgent.Shared.Contracts;

/// <summary>
/// Request raised via fdc3.raiseIntent to publish the raiseIntent request to the selected app.
/// </summary>
internal sealed class RaiseIntentResolutionRequest
{
    /// <summary>
    /// Unique identifier for the raised intent message which was generated from the received MessageId as int from the client and a <seealso cref="Guid"/>.
    /// </summary>
    public string MessageId { get; set; }

    /// <summary>
    /// Context which should be sent to the selected app via raiseIntent by the FDC3 client.
    /// </summary>
    [JsonConverter(typeof(ContextJsonConverter))]
    public string Context { get; set; }

    /// <summary>
    /// ContextMetadata which contains information about the source app, that raised the request.
    /// </summary>
    public ContextMetadata ContextMetadata { get; set; }
}