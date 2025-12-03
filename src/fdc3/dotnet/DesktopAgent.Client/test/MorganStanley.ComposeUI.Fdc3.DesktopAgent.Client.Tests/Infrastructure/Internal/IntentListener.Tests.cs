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
using Finos.Fdc3.Context;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.Client.Infrastructure.Internal;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.Shared;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.Shared.Contracts;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.Shared.Exceptions;
using MorganStanley.ComposeUI.Messaging.Abstractions;

namespace MorganStanley.ComposeUI.Fdc3.DesktopAgent.Client.Tests.Infrastructure.Internal;

public class IntentListener
{
    private readonly Mock<IMessaging> _messagingMock = new();
    private readonly Mock<IAsyncDisposable> _subscriptionMock = new();
    private readonly Mock<ILogger<IntentListener<Instrument>>> _loggerMock = new();
    private readonly string _intent = "ViewChart";
    private readonly string _instanceId = "testInstance";
    private readonly JsonSerializerOptions _jsonSerializerOptions = SerializerOptionsHelper.JsonSerializerOptionsWithContextSerialization;

    private IntentListener<Instrument> CreateIntentListener()
    {
        var intentListener = new IntentListener<Instrument>(
            _messagingMock.Object,
            _intent,
            _instanceId,
            (ctx, meta) => Task.FromResult<IIntentResult>(null!),
            _loggerMock.Object);
       
        return intentListener;
    }

    [Fact]
    public void Unsubscribe_throws_when_no_error_but_the_listener_is_still_stored()
    {
        var response = new IntentListenerResponse { Stored = true };
        _messagingMock
            .Setup(m => m.InvokeServiceAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(JsonSerializer.Serialize(response, _jsonSerializerOptions));

        _subscriptionMock
            .Setup(s => s.DisposeAsync())
            .Returns(ValueTask.CompletedTask)
            .Verifiable();

        var listener = CreateIntentListener();

        var act = () => listener.Unsubscribe();

        act.Should().Throw<Fdc3DesktopAgentException>()
            .WithMessage($"*Intent listener is still registered for the intent: {_intent}*");
    }

    [Fact]
    public void Unsubscribe_throws_when_no_response_received_from_server()
    {
        _messagingMock
            .Setup(m => m.InvokeServiceAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);

        var listener = CreateIntentListener();

        var act = () => listener.Unsubscribe();

        act.Should().Throw<Fdc3DesktopAgentException>()
            .WithMessage("*No response*");
    }

    [Fact]
    public void Unsubscribe_throws_when_error_response_received_from_the_server()
    {
        var response = new IntentListenerResponse { Error = "Some error" };
        _messagingMock
            .Setup(m => m.InvokeServiceAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(JsonSerializer.Serialize(response, _jsonSerializerOptions));

        var listener = CreateIntentListener();

        var act = () => listener.Unsubscribe();

        act.Should().Throw<Fdc3DesktopAgentException>()
            .WithMessage("*Some error*");
    }

    [Fact]
    public async Task Unsubscribe_disposes_once()
    {
        var unsubscribeResponse = new IntentListenerResponse { Stored = false };

        _messagingMock
            .SetupSequence(m => m.InvokeServiceAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(JsonSerializer.Serialize(unsubscribeResponse, _jsonSerializerOptions));

        _messagingMock
            .Setup(_ => _.SubscribeAsync(
                It.IsAny<string>(),
                It.IsAny<TopicMessageHandler>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(_subscriptionMock.Object);

        _subscriptionMock
            .Setup(_ => _.DisposeAsync())
            .Returns(ValueTask.CompletedTask);

        var listener = CreateIntentListener();

        await listener.RegisterIntentHandlerAsync();

        listener.Unsubscribe();

        _subscriptionMock.Verify(s => s.DisposeAsync(), Times.Once);
    }

    [Fact]
    public async Task Unsubscribe_throws_when_not_subscribed()
    {
        var unsubscribeResponse = new IntentListenerResponse { Stored = false };

        _messagingMock
            .SetupSequence(m => m.InvokeServiceAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(JsonSerializer.Serialize(unsubscribeResponse, _jsonSerializerOptions));

        var listener = CreateIntentListener();

        await listener.RegisterIntentHandlerAsync();

        var act = () => listener.Unsubscribe();

        act.Should().Throw<NullReferenceException>();
    }

    [Fact]
    public async Task RegisterIntentHandlerAsync_registers_handler()
    {
        _messagingMock
            .Setup(_ => _.SubscribeAsync(
                It.IsAny<string>(),
                It.IsAny<TopicMessageHandler>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(_subscriptionMock.Object);

        _subscriptionMock
            .Setup(_ => _.DisposeAsync())
            .Returns(ValueTask.CompletedTask);

        var listener = CreateIntentListener();

        var act = async () => await listener.RegisterIntentHandlerAsync();

        await act.Should().NotThrowAsync<Exception>();
    }

    [Fact]
    public async Task RegisterIntentHandlerAsync_registers_handler_only_once()
    {
        _messagingMock
            .Setup(_ => _.SubscribeAsync(
                It.IsAny<string>(),
                It.IsAny<TopicMessageHandler>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(_subscriptionMock.Object);

        _subscriptionMock
            .Setup(_ => _.DisposeAsync())
            .Returns(ValueTask.CompletedTask);

        var listener = CreateIntentListener();

        await listener.RegisterIntentHandlerAsync();
        await listener.RegisterIntentHandlerAsync();

        _messagingMock
            .Verify(_ => _.SubscribeAsync(
                It.IsAny<string>(),
                It.IsAny<TopicMessageHandler>(),
                It.IsAny<CancellationToken>()), Times.Once());
    }
}
