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

public class UserChannelErrorsAndDiagnosticsTests
{
    private class TestLogger : ILogger<UserChannel>
    {
        public int DebugCalls { get; private set; } = 0;
        public int WarningCalls { get; private set; } = 0;

        public IDisposable BeginScope<TState>(TState state)
        {
            throw new NotImplementedException();
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return true;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            switch (logLevel)
            {
                case LogLevel.Debug: DebugCalls++; break;
                case LogLevel.Warning: WarningCalls++; break;
            }
        }
    }

    private const string TestChannel = "testChannel";
    private readonly TestLogger _logger;
    private readonly UserChannel _channel;
    private readonly ChannelTopics _topics = Fdc3Topic.UserChannel(TestChannel);
    private readonly JsonSerializerOptions _jsonSerializerOptions = new(JsonSerializerDefaults.Web);

    public UserChannelErrorsAndDiagnosticsTests()
    {
        _logger = new TestLogger();
        _channel = new UserChannel(TestChannel, new Mock<IMessaging>().Object, _jsonSerializerOptions, _logger);
    }

    [Fact]
    public async Task EmptyPayloadBroadcastedIsLoggedAndIgnored()
    {
        await _channel.HandleBroadcast(EmptyBuffer);
        var ctx = await _channel.GetCurrentContext(null);

        ctx.Should().BeNull();
        VerifySingleWarning();
    }

    [Fact]
    public async Task NullPayloadBroadcastedIsLoggedAndIgnored()
    {
        await _channel.HandleBroadcast(EmptyBuffer);
        var ctx = await _channel.GetCurrentContext(null);
        ctx.Should().BeNull();
        VerifySingleWarning();
    }

    [Fact]
    public async Task NonJsonBroadcastedIsLoggedAndIgnored()
    {
        await _channel.HandleBroadcast(PlainTextBuffer);
        var ctx = await _channel.GetCurrentContext(null);
        ctx.Should().BeNull();
        VerifyDebugAndWarning();
    }

    [Fact]
    public async Task MissingContextTypeBroadcastedIsLoggedAndIgnored()
    {
        await _channel.HandleBroadcast(InvalidJsonBuffer);
        var ctx = await _channel.GetCurrentContext(null);
        ctx.Should().BeNull();
        VerifyDebugAndWarning();
    }

    private static string EmptyBuffer => string.Empty;
    private static string PlainTextBuffer => "Plain Text Payload";
    private static string InvalidJsonBuffer => "{\"randomField\":\"random text\"}";

    private void VerifyDebugAndWarning()
    {
        _logger.WarningCalls.Should().Be(1);
        _logger.DebugCalls.Should().Be(1);
    }

    private void VerifySingleWarning()
    {
        _logger.WarningCalls.Should().Be(1);
        _logger.DebugCalls.Should().Be(0);
    }

    [Fact]
    public void LogConnected_LoggerEnabled_LogsDebugMessage()
    {
        // Arrange
        var loggerMock = new Mock<ILogger>();
        loggerMock.Setup(l => l.IsEnabled(LogLevel.Debug)).Returns(true);

        var messagingMock = new Mock<IMessaging>();
        JsonSerializerOptions jsonSerializerOptions = new();

        var topics = new ChannelTopics("test", ChannelType.User);
        var channel = new TestChannel("test", messagingMock.Object, jsonSerializerOptions, loggerMock.Object, topics);

        // Act
        channel.CallLogConnected();

        // Assert
        loggerMock.Invocations
            .Should()
            .ContainSingle(invocation =>
                invocation.Method.Name == nameof(ILogger.Log) &&
                invocation.Arguments.Count > 2 &&
                (LogLevel) invocation.Arguments[0] == LogLevel.Debug &&
                invocation.Arguments[2].ToString()!.Contains("connected to the messaging service"));
    }

    [Fact]
    public void LogUnexpectedMessage_LoggerEnabled_LogsDebugMessage()
    {
        // Arrange
        var loggerMock = new Mock<ILogger>();
        loggerMock.Setup(l => l.IsEnabled(LogLevel.Warning)).Returns(true);

        var messagingMock = new Mock<IMessaging>();
        JsonSerializerOptions jsonSerializerOptions = new();

        var topics = new ChannelTopics("test", ChannelType.User);
        var channel = new TestChannel("test", messagingMock.Object, jsonSerializerOptions, loggerMock.Object, topics);
        var message = "Unexpected message received";

        // Act
        channel.CallLogUnexpectedError(message);

        // Assert
        loggerMock.Invocations
       .Should()
       .ContainSingle(invocation =>
           invocation.Method.Name == nameof(ILogger.Log) &&
           invocation.Arguments.Count > 2 &&
           (LogLevel) invocation.Arguments[0] == LogLevel.Warning &&
           invocation.Arguments[2].ToString()!.Contains("received unexpected message while trying to close a PrivateChannel"));
    }
}