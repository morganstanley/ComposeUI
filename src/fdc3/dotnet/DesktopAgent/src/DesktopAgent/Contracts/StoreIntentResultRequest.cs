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

using MorganStanley.Fdc3;
using MorganStanley.Fdc3.Context;

namespace MorganStanley.ComposeUI.Fdc3.DesktopAgent.Contracts;

/// <summary>
/// Request for the backend, indicating that the IntentHandler of the FDC3 client has executed the raised intent and handled it appropriately, 
/// and the backend should store and send back to the client which originated the request if the client calls the IntentResolution.getResult().
/// </summary>
internal sealed class StoreIntentResultRequest
{
    /// <summary>
    /// Unique identifier for the raised intent message which was a received MessageId as int type from the client and concatenated with a <seealso cref="Guid"/>.
    /// </summary>
    public string MessageId { get; set; }

    /// <summary>
    /// Intent from IntentResolution by IntentHandler of FDC3 clients.
    /// </summary>
    public string Intent { get; set; }

    /// <summary>
    /// Source app instance id which had handled the raised intent.
    /// </summary>
    public string OriginFdc3InstanceId { get; set; }

    /// <summary>
    /// Target app instance id which should receive the IntentResult that have raised the intent.
    /// </summary>
    public string TargetFdc3InstanceId { get; set; }

    /// <summary>
    /// ChannelId indicating the result of the IntentListener was a channel.
    /// </summary>
    public string? ChannelId {  get; set; }

    /// <summary>
    /// ChannelType indicating the result of the IntentListener was a channel. Clarifies the type of the Channel.
    /// </summary>
    public ChannelType? ChannelType { get; set; }

    /// <summary>
    /// Context indicating the result of the IntentListener was a context.
    /// </summary>
    public Context? Context { get; set; }

    /// <summary>
    /// Indicates that the result of the IntentHandler is void type.
    /// </summary>
    public bool? VoidResult { get; set; }

    /// <summary>
    /// Indicates that error occurred while resolving the raised intent via the registered IntentHandler.
    /// </summary>
    public string? Error { get; set; }
}
