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

using Microsoft.Extensions.Logging;
using MorganStanley.ComposeUI.Messaging.Abstractions;

namespace MorganStanley.ComposeUI.Fdc3.DesktopAgent.Tests;

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
    TestLogger _logger;
    UserChannel _channel;
    UserChannelTopics _topics = new UserChannelTopics(TestChannel);

    public UserChannelErrorsAndDiagnosticsTests()
    {
        _logger = new TestLogger();
        _channel = new UserChannel(TestChannel, new Mock<IMessagingService>().Object, _logger);
    }

    [Fact]
    public async void EmptyPayloadBroadcastedIsLoggedAndIgnored()
    {
        await _channel.HandleBroadcast(EmptyBuffer);
        var ctx = await _channel.GetCurrentContext(_topics.GetCurrentContext, null, null);

        ctx.Should().BeNull();
        VerifySingleWarning();
    }

    [Fact]
    public async void NullPayloadBroadcastedIsLoggedAndIgnored()
    {
        await _channel.HandleBroadcast(EmptyBuffer);
        var ctx = await _channel.GetCurrentContext(_topics.GetCurrentContext, null, null);
        ctx.Should().BeNull();
        VerifySingleWarning();
    }

    [Fact]
    public async void NonJsonBroadcastedIsLoggedAndIgnored()
    {
        await _channel.HandleBroadcast(PlainTextBuffer);
        var ctx = await _channel.GetCurrentContext(_topics.GetCurrentContext, null, null);
        ctx.Should().BeNull();
        VerifyDebugAndWarning();
    }

    [Fact]
    public async void MissingContextTypeBroadcastedIsLoggedAndIgnored()
    {
        await _channel.HandleBroadcast(InvalidJsonBuffer);
        var ctx = await _channel.GetCurrentContext(_topics.GetCurrentContext, null, null);
        ctx.Should().BeNull();
        VerifyDebugAndWarning();
    }

    private MessageBuffer EmptyBuffer => MessageBuffer.Create(string.Empty);
    private MessageBuffer PlainTextBuffer => MessageBuffer.Create("Plain Text Payload");
    private MessageBuffer InvalidJsonBuffer => MessageBuffer.Create("{\"randomField\":\"random text\"}");

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
}
