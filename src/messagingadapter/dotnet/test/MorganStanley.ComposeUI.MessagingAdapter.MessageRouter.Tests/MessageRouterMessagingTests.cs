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

using Moq;
using MorganStanley.ComposeUI.Messaging;
using MorganStanley.ComposeUI.Messaging.Abstractions;
using MorganStanley.ComposeUI.MessagingAdapter.Abstractions;

namespace MorganStanley.ComposeUI.MessagingAdapter.MessageRouter.Tests;

public class MessageRouterMessagingTests
{
    [Fact]
    public async Task ConnectAsync_ThrowsDuplicateEndpointException_WrapsAsAdapterException()
    {
        // Arrange
        var mockService = new Mock<IMessagingService>();
        mockService
            .Setup(s => s.ConnectAsync(It.IsAny<CancellationToken>()))
            .Returns(() => ValueTask.FromException(new MessageRouterDuplicateEndpointException("dup", "duplicate")));

        var adapter = new MessageRouterMessaging(mockService.Object);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<MessagingAdapterDuplicateEndpointException>(
            () => adapter.ConnectAsync().AsTask());
        Assert.Equal("dup", ex.Name);
        Assert.Equal("duplicate", ex.Message);
        Assert.IsType<MessageRouterDuplicateEndpointException>(ex.InnerException);
    }

    [Fact]
    public async Task ConnectAsync_ThrowsRouterException_WrapsAsAdapterException()
    {
        // Arrange
        var mockService = new Mock<IMessagingService>();
        mockService
            .Setup(s => s.ConnectAsync(It.IsAny<CancellationToken>()))
            .Returns(() => ValueTask.FromException(new MessageRouterException("router", "router error")));

        var adapter = new MessageRouterMessaging(mockService.Object);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<MessagingAdapterException>(
            () => adapter.ConnectAsync().AsTask());
        Assert.Equal("router", ex.Name);
        Assert.Equal("router error", ex.Message);
        Assert.IsType<MessageRouterException>(ex.InnerException);
    }

    [Fact]
    public async Task InvokeAsync_ThrowsDuplicateEndpointException_WrapsAsAdapterException()
    {
        // Arrange
        var mockService = new Mock<IMessagingService>();
        mockService
            .Setup(s => s.InvokeAsync(
                It.IsAny<string>(),
                It.IsAny<IMessageBuffer?>(),
                It.IsAny<Messaging.Abstractions.InvokeOptions>(),
                It.IsAny<CancellationToken>()))
            .Returns(() => ValueTask.FromException<IMessageBuffer?>(new MessageRouterDuplicateEndpointException("dup", "duplicate")));

        var adapter = new MessageRouterMessaging(mockService.Object);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<MessagingAdapterDuplicateEndpointException>(
            () => adapter.InvokeAsync("endpoint", "payload").AsTask());
        Assert.Equal("dup", ex.Name);
        Assert.Equal("duplicate", ex.Message);
        Assert.IsType<MessageRouterDuplicateEndpointException>(ex.InnerException);
    }

    [Fact]
    public async Task InvokeAsync_ThrowsRouterException_WrapsAsAdapterException()
    {
        // Arrange
        var mockService = new Mock<IMessagingService>();
        mockService
            .Setup(s => s.InvokeAsync(
                It.IsAny<string>(),
                It.IsAny<IMessageBuffer?>(),
                It.IsAny<Messaging.Abstractions.InvokeOptions>(),
                It.IsAny<CancellationToken>()))
            .Returns(() => ValueTask.FromException<IMessageBuffer?>(new MessageRouterException("router", "router error")));

        var adapter = new MessageRouterMessaging(mockService.Object);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<MessagingAdapterException>(
            () => adapter.InvokeAsync("endpoint", "payload").AsTask());
        Assert.Equal("router", ex.Name);
        Assert.Equal("router error", ex.Message);
        Assert.IsType<MessageRouterException>(ex.InnerException);
    }
}