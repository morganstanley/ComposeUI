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
using MorganStanley.Fdc3;
using MorganStanley.Fdc3.Context;

namespace MorganStanley.ComposeUI.Fdc3.DesktopAgent.Contracts;

/// <summary>
/// Request, originated via fdc.raiseIntent by the client.
/// </summary>
public class RaiseIntentRequest
{
    [JsonConstructor]
    public RaiseIntentRequest(
        int raiseIntentMessageId,
        string fdc3InstanceId,
        string intent,
        bool selected,
        Context? context = null,
        AppIdentifier? appIdentifier = null,
        string? error = null)
    {
        RaiseIntentMessageId = raiseIntentMessageId;
        Fdc3InstanceId = fdc3InstanceId;
        Intent = intent;
        Selected = selected;
        Context = context ?? new Context("fdc3.nothing");
        AppIdentifier = appIdentifier;
        Error = error;
    }

    /// <summary>
    /// Unique identifier for the message.
    /// </summary>
    public int RaiseIntentMessageId { get; }

    /// <summary>
    /// Unique identifier from the application which sent the RaiseIntentRequest type of message.
    /// Probably the instanceId of the application which can be queried from the window.object, etc.
    /// </summary>
    public string Fdc3InstanceId { get; }

    /// <summary>
    /// Intent, that has been raised.
    /// </summary>
    public string Intent { get; }

    /// <summary>
    /// Indicates that the client selected an instance or an app to start and resolve the raised intent.
    /// </summary>
    public bool Selected { get; }

    /// <summary>
    /// Context, for identifying more the specific app that should handle the raised intent.
    /// </summary>
    public Context Context { get; }

    /// <summary>
    /// Information about the app, that should resolve the raised intent.
    /// </summary>
    public AppIdentifier? AppIdentifier { get; }

    /// <summary>
    /// Error, which indicates, that some error happened during executing the raiseIntent method.
    /// </summary>
    public string? Error { get; }
}
