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

namespace MorganStanley.ComposeUI.Fdc3.DesktopAgent.Infrastructure.Internal;

internal class ContextListener
{
    private readonly string? _contextType;
    private readonly Guid _instanceId;
    private string? _channelId;
    private ChannelType? _channelType;

    public Guid Id => _instanceId;
    public string? ContextType => _contextType;

    public ContextListener(string? contextType, string channelId, ChannelType channelType)
    {
        _contextType = contextType;
        _channelId = channelId;
        _channelType = channelType;
        _instanceId = Guid.NewGuid();
    }
}