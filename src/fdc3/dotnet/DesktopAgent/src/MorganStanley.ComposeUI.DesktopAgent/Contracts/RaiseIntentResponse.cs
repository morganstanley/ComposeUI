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

using Finos.Fdc3;
using AppMetadata = MorganStanley.ComposeUI.Fdc3.DesktopAgent.Protocol.AppMetadata;
using AppIntent = MorganStanley.ComposeUI.Fdc3.DesktopAgent.Protocol.AppIntent;

namespace MorganStanley.ComposeUI.Fdc3.DesktopAgent.Contracts;

/// <summary>
/// Response for handling <see cref="RaiseIntentRequest"/> raised via fdc.raiseIntent by client.
/// </summary>
internal sealed class RaiseIntentResponse
{
    /// <summary>
    /// Unique identifier for the raised intent message which was generated from the received MessageId as type int from the client and a <seealso cref="Guid"/>.
    /// </summary>
    public string? MessageId { get; set; }

    /// <summary>
    /// Intent that have been raised.
    /// </summary>
    public string? Intent { get; set; }

    /// <summary>
    /// Apps that could handle the raiseIntent.
    /// </summary>
    public AppMetadata? AppMetadata { get; set; }

    /// <summary>
    /// Contains error text if an error happened during the raiseIntent's execution.
    /// </summary>
    public string? Error { get; set; }

    public static RaiseIntentResponse Success(string messageId, string intent, IAppMetadata appMetadata) => new() { MessageId = messageId, Intent = intent, AppMetadata = (AppMetadata)appMetadata };
    public static RaiseIntentResponse Success(string messageId, string intent, AppMetadata appMetadata) => new() {MessageId = messageId, Intent = intent, AppMetadata = appMetadata };
    public static RaiseIntentResponse Failure(string error) => new() { Error = error };
}
