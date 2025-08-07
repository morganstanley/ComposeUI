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
using Finos.Fdc3;
using Microsoft.Extensions.Logging;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.Channels;
using MorganStanley.ComposeUI.Messaging.Abstractions;

namespace MorganStanley.ComposeUI.Fdc3.DesktopAgent.Tests.Channels;

public class UserChannelTests : ChannelTestBase
{
    private const string TestChannel = "testChannel";

    public UserChannelTests()
    {
        Channel = new UserChannel(TestChannel, new Mock<IMessaging>().Object, new JsonSerializerOptions(JsonSerializerDefaults.Web), null);
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

        JsonSerializerOptions jsonSerializerOptions = new();

        var channel = new TestChannel(TestChannel, messagingMock.Object, jsonSerializerOptions, loggerMock.Object, topics);

        // Act
        await channel.CallHandleBroadcast(null);

        // Assert
        loggerMock.Invocations
        .Should()
        .ContainSingle(invocation =>
            invocation.Method.Name == nameof(ILogger.Log) &&
            invocation.Arguments.Count > 2 &&
            (LogLevel) invocation.Arguments[0] == LogLevel.Warning &&
            invocation.Arguments[2].ToString()!.Contains("received a null or empty payload in broadcast")
        );
    }

    [Fact]
    public async Task Connect_AfterDispose_ThrowsObjectDisposedException()
    {
        // Arrange
        var loggerMock = new Mock<ILogger>();
        var messagingMock = new Mock<IMessaging>();
        var topics = new ChannelTopics("test", ChannelType.User);

        JsonSerializerOptions jsonSerializerOptions = new();

        var channel = new TestChannel("test", messagingMock.Object, jsonSerializerOptions, loggerMock.Object, topics);

        // Act
        await channel.DisposeAsync();

        // Assert
        Func<Task> act = async () => await channel.Connect().AsTask();
        await act.Should().ThrowAsync<ObjectDisposedException>();
    }
}