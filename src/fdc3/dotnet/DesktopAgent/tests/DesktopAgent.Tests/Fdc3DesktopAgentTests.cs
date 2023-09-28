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

using Microsoft.Extensions.Logging.Abstractions;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.Contracts;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.DependencyInjection;
using MorganStanley.Fdc3;

namespace MorganStanley.ComposeUI.Fdc3.DesktopAgent.Tests;

public class Fdc3DesktopAgentTests
{
    private Fdc3DesktopAgent _fdc3 = new Fdc3DesktopAgent(
        new Fdc3DesktopAgentOptions(), 
        new Mock<IMessageRouter>().Object, 
        NullLoggerFactory.Instance);
    private const string TestChannel = "testChannel";

    [Fact]
    public async void UserChannelAddedCanBeFound()
    {
        await _fdc3.AddUserChannel(TestChannel);

        var result = await _fdc3.FindChannel(Fdc3Topic.FindChannel, FindTestChannel, new MessageContext());

        result.Should().NotBeNull();
        result.ReadJson<FindChannelResponse>().Should().BeEquivalentTo(FindChannelResponse.Success);
    }

    private MessageBuffer FindTestChannel => MessageBuffer.Factory.CreateJson(new FindChannelRequest() { ChannelId = "testChannel", ChannelType = ChannelType.User });
}
