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


using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.Shared;
using MorganStanley.ComposeUI.Messaging.Abstractions;

namespace MorganStanley.ComposeUI.Fdc3.DesktopAgent.Channels;

internal class AppChannel : Channel
{
    public AppChannel(string id, IMessaging messagingService, JsonSerializerOptions jsonSerializerOptions, ILogger<AppChannel>? logger = null)
        : base(id, messagingService, jsonSerializerOptions, logger ?? NullLogger<AppChannel>.Instance, Fdc3Topic.AppChannel(id)) { }

    protected override string ChannelTypeName => nameof(AppChannel);
}
