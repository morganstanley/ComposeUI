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
using Microsoft.Extensions.Logging;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.Channels;
using MorganStanley.ComposeUI.MessagingAdapter.Abstractions;

namespace MorganStanley.ComposeUI.Fdc3.DesktopAgent.Tests.Channels;

public class UserChannelTests : ChannelTestBase
{
    private const string TestChannel = "testChannel";

    public UserChannelTests()
    {
        Channel = new UserChannel(TestChannel, new Mock<IMessaging>().Object, null);
        Topics = Fdc3Topic.UserChannel(TestChannel);
    }

    [Fact]
    public async Task HandleBroadcast_NullPayload_LogsWarning()
    {
        // Arrange
        var loggerMock = new Mock<ILogger>();
        loggerMock.Setup(l => l.IsEnabled(LogLevel.Warning)).Returns(true);

        var messagingMock = new Mock<IMessaging>();
        var topics = new ChannelTopics(TestChannel, ChannelType.User);

        var channel = new TestChannel(TestChannel, messagingMock.Object, loggerMock.Object, topics);

        // Act
        await channel.CallHandleBroadcast(null);

        // Assert
        loggerMock.Verify(
            l => l.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("received a null or empty payload in broadcast")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task Connect_AfterDispose_ThrowsObjectDisposedException()
    {
        // Arrange
        var loggerMock = new Mock<ILogger>();
        var messagingMock = new Mock<IMessaging>();
        var topics = new ChannelTopics("test", ChannelType.User);

        var channel = new TestChannel("test", messagingMock.Object, loggerMock.Object, topics);

        // Act
        await channel.DisposeAsync();

        // Assert
        await Assert.ThrowsAsync<ObjectDisposedException>(() => channel.Connect().AsTask());
    }
}