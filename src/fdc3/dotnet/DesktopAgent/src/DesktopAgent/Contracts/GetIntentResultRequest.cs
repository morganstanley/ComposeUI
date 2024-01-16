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


using AppIdentifier = MorganStanley.ComposeUI.Fdc3.DesktopAgent.Protocol.AppIdentifier;

namespace MorganStanley.ComposeUI.Fdc3.DesktopAgent.Contracts;

/// <summary>
/// Request raised by the client via calling the IntentResolution.getResult().
/// </summary>
internal sealed class GetIntentResultRequest
{
    /// <summary>
    /// IntentResolution's stored MessageId received via <seealso cref="RaiseIntentResponse"/>.
    /// </summary>
    public string MessageId { get; set; }

    /// <summary>
    /// The intent that was raised.
    /// </summary>
    public string Intent { get; set; }

    /// <summary>
    /// <seealso cref="AppIdentifier"/> for the app instance that was selected (or started) to resolve the intent.
    /// </summary>
    public AppIdentifier TargetAppIdentifier { get; set; }

    /// <summary>
    /// The version number of the Intents schema, that is being used.
    /// </summary>
    public string? Version { get; set; }
}
