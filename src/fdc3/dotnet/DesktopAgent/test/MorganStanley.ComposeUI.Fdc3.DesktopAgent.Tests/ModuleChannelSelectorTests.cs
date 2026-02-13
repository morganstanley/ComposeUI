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

using MorganStanley.ComposeUI.Fdc3.DesktopAgent.Shared;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.Shared.Exceptions;
using MorganStanley.ComposeUI.Messaging.Abstractions;
using ServiceHandler = MorganStanley.ComposeUI.Messaging.Abstractions.ServiceHandler;

namespace MorganStanley.ComposeUI.Fdc3.DesktopAgent.Tests;

public class ModuleChannelSelectorTests
{
    private readonly Mock<IMessaging> _mockMessaging = new();

    [Fact]
    public async Task RegisterChannelSelectorHandlerInitiatedFromClientsAsync_registers_service()
    {
        var moduleChannelSelector = new ModuleChannelSelector(_mockMessaging.Object);
        var fdc3InstanceId = "test-instance-id";

        Action<string?> onChannelJoined = (channelId) => { };

        _mockMessaging
            .Setup(m => m.RegisterServiceAsync(
                It.IsAny<string>(),
                It.IsAny<ServiceHandler>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Mock.Of<IAsyncDisposable>());

        await moduleChannelSelector.RegisterChannelSelectorHandlerInitiatedFromClientsAsync(
            fdc3InstanceId,
            onChannelJoined);

        _mockMessaging.Verify(m => m.RegisterServiceAsync(
            It.Is<string>(s => s == Fdc3Topic.ChannelSelectorFromAPI(fdc3InstanceId)),
            It.IsAny<ServiceHandler>(),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task RegisterChannelSelectorHandlerInitiatedFromClientsAsync_throws_exception_when_instanceId_is_null_or_empty()
    {
        var moduleChannelSelector = new ModuleChannelSelector(_mockMessaging.Object);
        Action<string?> onChannelJoined = (channelId) => { };

        var action1 = async() => await moduleChannelSelector.RegisterChannelSelectorHandlerInitiatedFromClientsAsync(
            null!,
            onChannelJoined);

        await action1.Should().ThrowAsync<Fdc3DesktopAgentException>();

        var action2 = async() => await moduleChannelSelector.RegisterChannelSelectorHandlerInitiatedFromClientsAsync(
            string.Empty,
            onChannelJoined);

        await action2.Should().ThrowAsync<Fdc3DesktopAgentException>();
    }

    [Fact]
    public async Task InvokeJoinUserChannelFromUIAsync_invokes_service_and_returns_channelId()
    {
        var moduleChannelSelector = new ModuleChannelSelector(_mockMessaging.Object);
        var fdc3InstanceId = "test-instance-id";
        var channelId = "test-channel-id";

        _mockMessaging
            .Setup(m => m.InvokeServiceAsync(
                It.IsAny<string>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(channelId);

        var result = await moduleChannelSelector.InvokeJoinUserChannelFromUIAsync(
            fdc3InstanceId,
            channelId);

        result.Should().NotBeNull();
        result.Should().Be(channelId);
    }
}
