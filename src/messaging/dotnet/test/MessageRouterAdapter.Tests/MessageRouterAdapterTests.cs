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

using FluentAssertions;
using Moq;
using MorganStanley.ComposeUI.Messaging;
using MorganStanley.ComposeUI.Messaging.Abstractions;
using MorganStanley.ComposeUI.Messaging.Abstractions.Exceptions;
using MorganStanley.ComposeUI.MessagingAdapter;

namespace MorganStanley.ComposeUI.MessageRouterAdapter.Tests;

public class MessageRouterAdapterTests
{
    [Fact]
    public async Task ConnectAsync_ThrowsDuplicateEndpointException_WrapsAsAdapterException()
    {
        // Arrange
        var mockService = new Mock<IMessageRouter>();
        mockService
            .Setup(s => s.SubscribeAsync(It.IsAny<string>(), It.IsAny<MessageHandler>(), It.IsAny<CancellationToken>()))
            .Returns(() => new ValueTask<IAsyncDisposable>(Task.FromException<IAsyncDisposable>(new MessageRouterDuplicateEndpointException("dup", "duplicate"))));

        var adapter = new MessageRouterMessaging(mockService.Object);

        var topicMessageHandler = new TopicMessageHandler((message) => ValueTask.CompletedTask);

        // Act
        Func<Task> act = () => adapter.SubscribeAsync("topic", topicMessageHandler, CancellationToken.None).AsTask();

        // Assert
        var ex = await act.Should().ThrowAsync<DuplicateServiceNameException>();
        ex.Which.Name.Should().Be("dup");
        ex.Which.Message.Should().Be("duplicate");
        ex.Which.InnerException.Should().BeOfType<MessageRouterDuplicateEndpointException>();
    }

    [Fact]
    public async Task ConnectAsync_ThrowsRouterException_WrapsAsAdapterException()
    {
        // Arrange
        var mockService = new Mock<IMessageRouter>();
        mockService
            .Setup(s => s.SubscribeAsync(It.IsAny<string>(), It.IsAny<MessageHandler>(), It.IsAny<CancellationToken>()))
            .Returns(() => new ValueTask<IAsyncDisposable>(Task.FromException<IAsyncDisposable>(new MessageRouterException("router", "router error"))));

        var adapter = new MessageRouterMessaging(mockService.Object);

        var topicMessageHandler = new TopicMessageHandler((message) => ValueTask.CompletedTask);

        // Act
        Func<Task> act = () => adapter.SubscribeAsync("topic", topicMessageHandler, CancellationToken.None).AsTask();

        // Assert
        var ex = await act.Should().ThrowAsync<MessagingException>();
        ex.Which.Name.Should().Be("router");
        ex.Which.Message.Should().Be("router error");
        ex.Which.InnerException.Should().BeOfType<MessageRouterException>();
    }

    [Fact]
    public async Task InvokeAsync_ThrowsDuplicateEndpointException_WrapsAsAdapterException()
    {
        // Arrange
        var mockService = new Mock<IMessageRouter>();
        mockService
            .Setup(s => s.InvokeAsync(
                It.IsAny<string>(),
                It.IsAny<IMessageBuffer?>(),
                It.IsAny<InvokeOptions>(),
                It.IsAny<CancellationToken>()))
            .Returns(() => ValueTask.FromException<IMessageBuffer?>(new MessageRouterDuplicateEndpointException("dup", "duplicate")));

        var adapter = new MessageRouterMessaging(mockService.Object);

        // Act
        Func<Task> act = () => adapter.InvokeServiceAsync("endpoint", "payload").AsTask();

        // Assert
        var ex = await act.Should().ThrowAsync<DuplicateServiceNameException>();
        ex.Which.Name.Should().Be("dup");
        ex.Which.Message.Should().Be("duplicate");
        ex.Which.InnerException.Should().BeOfType<MessageRouterDuplicateEndpointException>();
    }

    [Fact]
    public async Task InvokeAsync_ThrowsRouterException_WrapsAsAdapterException()
    {
        // Arrange
        var mockService = new Mock<IMessageRouter>();
        mockService
            .Setup(s => s.InvokeAsync(
                It.IsAny<string>(),
                It.IsAny<IMessageBuffer?>(),
                It.IsAny<InvokeOptions>(),
                It.IsAny<CancellationToken>()))
            .Returns(() => ValueTask.FromException<IMessageBuffer?>(new MessageRouterException("router", "router error")));

        var adapter = new MessageRouterMessaging(mockService.Object);

        // Act
        Func<Task> act = () => adapter.InvokeServiceAsync("endpoint", "payload").AsTask();

        // Assert
        var ex = await act.Should().ThrowAsync<MessagingException>();
        ex.Which.Name.Should().Be("router");
        ex.Which.Message.Should().Be("router error");
        ex.Which.InnerException.Should().BeOfType<MessageRouterException>();
    }
}