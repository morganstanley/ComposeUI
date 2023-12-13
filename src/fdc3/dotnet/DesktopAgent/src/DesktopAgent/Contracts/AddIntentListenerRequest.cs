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

namespace MorganStanley.ComposeUI.Fdc3.DesktopAgent.Contracts;

/// <summary>
/// Request originated by the client, which indicates that it would like to add or remove its IntentListener.
/// </summary>
internal sealed class AddIntentListenerRequest
{
    /// <summary>
    /// State, that indicates the action, that the client;s listener would like to take, lik subscribe or unsubscribe from an intent.
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public SubscribeState State { get; set; } 

    /// <summary>
    /// Intent, for the listener, which would like to (un)subscribe.
    /// </summary>
    public string Intent { get; set; }

    /// <summary>
    /// Fdc3 specific insatnce id from the client, which would like to add/renive its intent listener.
    /// </summary>
    public string Fdc3InstanceId { get; set; }

}
