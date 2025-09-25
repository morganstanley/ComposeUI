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
using FluentAssertions;
using Moq;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.Client.Infrastructure;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.Shared.Contracts;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.Shared.Exceptions;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.Shared.Protocol;
using MorganStanley.ComposeUI.Messaging.Abstractions;

namespace MorganStanley.ComposeUI.Fdc3.DesktopAgent.Client.Tests.Infrastructure;

public class DesktopAgentClientTests : IAsyncLifetime
{
    [Fact]
    public async Task GetAppMetadata_returns_null_response()
    {
        var messagingMock = new Mock<IMessaging>();

        messagingMock.Setup(
                _ => _.InvokeServiceAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
            .Returns(null);

        var desktopAgent = new DesktopAgentClient(messagingMock.Object);

        var action = async () => await desktopAgent.GetAppMetadata(new AppIdentifier { AppId = "test-appId" });

        await action.Should().ThrowAsync<Fdc3DesktopAgentException>()
            .WithMessage(Fdc3DesktopAgentErrors.NoResponse);
    }

    [Fact]
    public async Task GetAppMetadata_returns_error_response()
    {
        var messagingMock = new Mock<IMessaging>();

        messagingMock.Setup(
                _ => _.InvokeServiceAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
            .Returns(new ValueTask<string?>(JsonSerializer.Serialize(new GetAppMetadataResponse { Error = "test-error" })));

        var desktopAgent = new DesktopAgentClient(messagingMock.Object);

        var action = async () => await desktopAgent.GetAppMetadata(new AppIdentifier { AppId = "test-appId" });

        await action.Should().ThrowAsync<Fdc3DesktopAgentException>()
            .WithMessage("*test-error*");
    }

    [Fact]
    public async Task GetAppMetadata_returns_AppIdentifier()
    {
        var messagingMock = new Mock<IMessaging>();

        messagingMock.Setup(
                _ => _.InvokeServiceAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
            .Returns(new ValueTask<string?>(JsonSerializer.Serialize(new GetAppMetadataResponse { AppMetadata = new AppMetadata { AppId = "test-appId" } })));

        var desktopAgent = new DesktopAgentClient(messagingMock.Object);

        var result =  await desktopAgent.GetAppMetadata(new AppIdentifier { AppId = "test-appId" });

        result.Should().NotBeNull();
        result!.Should().BeEquivalentTo(new AppMetadata {AppId = "test-appId"});
    }

    [Fact]
    public async Task GetInfo_throws_error_as_no_response_received()
    {
        var messagingMock = new Mock<IMessaging>();

        messagingMock.Setup(
                _ => _.InvokeServiceAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
            .Returns(null);

        var desktopAgent = new DesktopAgentClient(messagingMock.Object);

        var action = async () => await desktopAgent.GetInfo();

        await action.Should().ThrowAsync<Fdc3DesktopAgentException>()
            .WithMessage("*can't return the information about the initiator app*");
    }

    [Fact]
    public async Task GetInfo_throws_error_as_error_response_received()
    {
        var messagingMock = new Mock<IMessaging>();

        messagingMock.Setup(
                _ => _.InvokeServiceAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
            .Returns(new ValueTask<string?>(JsonSerializer.Serialize(new GetInfoResponse { Error = "test-error" })));

        var desktopAgent = new DesktopAgentClient(messagingMock.Object);

        var action = async () => await desktopAgent.GetInfo();

        await action.Should().ThrowAsync<Fdc3DesktopAgentException>()
            .WithMessage("*test-error*");
    }

    [Fact]
    public async Task GetInfo_returns_ImplementationMetadata()
    {
        var messagingMock = new Mock<IMessaging>();
        var implementationMetadata = new ImplementationMetadata {AppMetadata = new AppMetadata {AppId = "test_appId"}};

        messagingMock.Setup(
                _ => _.InvokeServiceAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
            .Returns(new ValueTask<string?>(JsonSerializer.Serialize(new GetInfoResponse { ImplementationMetadata = implementationMetadata })));

        var desktopAgent = new DesktopAgentClient(messagingMock.Object);

        var result = await desktopAgent.GetInfo();

        result.Should().NotBeNull();
        result.Should().BeEquivalentTo(implementationMetadata);
    }

    public Task InitializeAsync()
    {
        Environment.SetEnvironmentVariable(nameof(AppIdentifier.AppId), "test-appId2");
        Environment.SetEnvironmentVariable(nameof(AppIdentifier.InstanceId), Guid.NewGuid().ToString());

        return Task.CompletedTask;
    }

    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }
}