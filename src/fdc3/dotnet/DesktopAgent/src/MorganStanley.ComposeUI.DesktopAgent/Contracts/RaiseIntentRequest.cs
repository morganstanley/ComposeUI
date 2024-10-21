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

using Finos.Fdc3.Context;
using AppIdentifier = MorganStanley.ComposeUI.Fdc3.DesktopAgent.Protocol.AppIdentifier;

namespace MorganStanley.ComposeUI.Fdc3.DesktopAgent.Contracts;

/// <summary>
/// Request raised by the client via fdc.raiseIntent.
/// </summary>
internal sealed class RaiseIntentRequest
{
    /// <summary>
    /// An identifier for the message.
    /// </summary>
    public int MessageId { get; set; }

    /// <summary>
    /// Unique identifier from the application which sent the RaiseIntentRequest type of message.
    /// eg.: the instanceId of the application which can be queried from the window.object.composeui.fdc3.config, if it's a WebApplication.
    /// </summary>
    public string Fdc3InstanceId { get; set; }

    /// <summary>
    /// Intent that has been raised.
    /// </summary>
    public string Intent { get; set; }

    /// <summary>
    /// Context for identifying more the specific app that should handle the raised intent.
    /// </summary>
    public Context? Context { get; set; }

    /// <summary>
    /// Information about the app that should resolve the raised intent.
    /// </summary>
    public AppIdentifier? TargetAppIdentifier { get; set; }
}
