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

public class AddIntentListenerRequest
{
    [JsonConstructor]
    public AddIntentListenerRequest(
        string intent,
        string fdc3InstanceId,
        SubscribeState state)
    {
        Intent = intent;
        Fdc3InstanceId = fdc3InstanceId;
        State = state;
    }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public SubscribeState State { get; } 
    public string Intent { get; }
    public string Fdc3InstanceId { get; }

}
