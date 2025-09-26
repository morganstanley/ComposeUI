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
using FluentAssertions;
using Moq;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.Client.Infrastructure.Internal;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.Shared;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.Shared.Contracts;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.Shared.Exceptions;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.Shared.Protocol;
using MorganStanley.ComposeUI.Messaging.Abstractions;
using AppIdentifier = MorganStanley.ComposeUI.Fdc3.DesktopAgent.Shared.Protocol.AppIdentifier;

namespace MorganStanley.ComposeUI.Fdc3.DesktopAgent.Client.Tests.Infrastructure.Internal;

public class MetadataClientTests
{
    private const string AppId = "test-app";
    private const string InstanceId = "test-instance";
    private readonly JsonSerializerOptions _jsonSerializerOptions = SerializerOptionsHelper.JsonSerializerOptionsWithContextSerialization;

    [Fact]
    public async Task GetAppMetadataAsync_ReturnsAppMetadata_WhenResponseIsValid()
    {
        var appIdentifier = Mock.Of<IAppIdentifier>(a =>
            a.AppId == AppId && a.InstanceId == InstanceId);

        var expectedMetadata = new Shared.Protocol.AppMetadata
        {
            AppId = "test-id",
            InstanceId = Guid.NewGuid().ToString()
        };

        var response = new GetAppMetadataResponse
        {
            AppMetadata = expectedMetadata
        };

        var messagingMock = new Mock<IMessaging>();
        messagingMock
            .Setup(
                _ => _.InvokeServiceAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
            .Returns(new ValueTask<string?>(JsonSerializer.Serialize(response, _jsonSerializerOptions)));

        var client = new MetadataClient(AppId, InstanceId, messagingMock.Object);

        var result = await client.GetAppMetadataAsync(appIdentifier);

        result.Should().BeEquivalentTo(expectedMetadata);
    }

    [Fact]
    public async Task GetAppMetadataAsync_ThrowsMissingResponseException_WhenResponseIsNull()
    {
        var appIdentifier = Mock.Of<IAppIdentifier>(a =>
            a.AppId == AppId && a.InstanceId == InstanceId);

        var messagingMock = new Mock<IMessaging>();
        messagingMock
            .Setup(
                _ => _.InvokeServiceAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
            .Returns(new ValueTask<string?>((string?)null));

        var client = new MetadataClient(AppId, InstanceId, messagingMock.Object);

        var act = async () => await client.GetAppMetadataAsync(appIdentifier);

        await act.Should().ThrowAsync<Fdc3DesktopAgentException>()
            .WithMessage("No response was received from the FDC3 backend server.");
    }

    [Fact]
    public async Task GetAppMetadataAsync_ThrowsErrorResponseReceivedException_WhenResponseHasError()
    {
        var appIdentifier = Mock.Of<IAppIdentifier>(a =>
            a.AppId == AppId && a.InstanceId == InstanceId);

        var response = new GetAppMetadataResponse
        {
            Error = "Some error"
        };

        var messagingMock = new Mock<IMessaging>();
        messagingMock
            .Setup(
                _ => _.InvokeServiceAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
            .Returns(new ValueTask<string?>(JsonSerializer.Serialize(response, _jsonSerializerOptions)));

        var client = new MetadataClient(AppId, InstanceId, messagingMock.Object);

        var act = async () => await client.GetAppMetadataAsync(appIdentifier);

        await act.Should().ThrowAsync<Fdc3DesktopAgentException>()
            .WithMessage("*Some error*");
    }

    [Fact]
    public async Task GetInfoAsync_ReturnsImplementationMetadata_WhenResponseIsValid()
    {
        var expectedMetadata = new Shared.Protocol.ImplementationMetadata
        {
            AppMetadata = new Shared.Protocol.AppMetadata
            {
                AppId = "test-id",
                InstanceId = Guid.NewGuid().ToString()
            },
        };

        var response = new GetInfoResponse
        {
            ImplementationMetadata = expectedMetadata
        };

        var messagingMock = new Mock<IMessaging>();
        messagingMock
            .Setup(
                _ => _.InvokeServiceAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
            .Returns(new ValueTask<string?>(JsonSerializer.Serialize(response, _jsonSerializerOptions)));

        var client = new MetadataClient(AppId, InstanceId, messagingMock.Object);

        var result = await client.GetInfoAsync();

        result.Should().BeEquivalentTo(expectedMetadata);
    }

    [Fact]
    public async Task GetInfoAsync_ThrowsMissingResponseException_WhenResponseIsNull()
    {
        var messagingMock = new Mock<IMessaging>();
        messagingMock
            .Setup(
                _ => _.InvokeServiceAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
            .Returns(new ValueTask<string?>((string?)null));

        var client = new MetadataClient(AppId, InstanceId, messagingMock.Object);

        var act = async () => await client.GetInfoAsync();

        await act.Should().ThrowAsync<Fdc3DesktopAgentException>()
            .WithMessage("*No response was received from the FDC3 backend server.*");
    }

    [Fact]
    public async Task GetInfoAsync_ThrowsErrorResponseReceivedException_WhenResponseHasError()
    {
        var response = new GetInfoResponse
        {
            Error = "Some error"
        };

        var messagingMock = new Mock<IMessaging>();
        messagingMock
            .Setup(
                _ => _.InvokeServiceAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
            .Returns(new ValueTask<string?>(JsonSerializer.Serialize(response, _jsonSerializerOptions)));

        var client = new MetadataClient(AppId, InstanceId, messagingMock.Object);

        var act = async() => await client.GetInfoAsync();

        await act.Should().ThrowAsync<Fdc3DesktopAgentException>()
            .WithMessage("*Some error*");
    }

    [Fact]
    public async Task FindInstancesAsync_returns_instances_when_successful()
    {
        var appIdentifier = new AppIdentifier { AppId = "app", InstanceId = "id" };
        var expectedInstances = new[] { appIdentifier };
        var response = new FindInstancesResponse
        {
            Instances = expectedInstances
        };

        var messagingMock = new Mock<IMessaging>();
        messagingMock
            .Setup(m => m.InvokeServiceAsync(
                Fdc3Topic.FindInstances,
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .Returns(new ValueTask<string?>(JsonSerializer.Serialize(response, _jsonSerializerOptions)));

        var client = new MetadataClient(AppId, InstanceId, messagingMock.Object);
        var result = await client.FindInstancesAsync(appIdentifier);

        result.Should().BeEquivalentTo(expectedInstances);
    }

    [Fact]
    public async Task FindInstancesAsync_throws_when_response_is_null()
    {
        var appIdentifier = new AppIdentifier { AppId = "app", InstanceId = "id" };

        var messagingMock = new Mock<IMessaging>();
        messagingMock
            .Setup(m => m.InvokeServiceAsync(
                Fdc3Topic.FindInstances,
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .Returns(new ValueTask<string?>((string?) null));

        var client = new MetadataClient(AppId, InstanceId, messagingMock.Object);
        var act = async () => await client.FindInstancesAsync(appIdentifier);

        await act.Should().ThrowAsync<Fdc3DesktopAgentException>()
            .WithMessage("*No response was received from the FDC3 backend server*");
    }

    [Fact]
    public async Task FindInstancesAsync_throws_when_response_has_error()
    {
        var appIdentifier = new AppIdentifier { AppId = "app", InstanceId = "id" };
        var response = new FindInstancesResponse
        {
            Error = "Some error"
        };

        var messagingMock = new Mock<IMessaging>();
        messagingMock
            .Setup(m => m.InvokeServiceAsync(
                Fdc3Topic.FindInstances,
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .Returns(new ValueTask<string?>(JsonSerializer.Serialize(response, _jsonSerializerOptions)));

        var client = new MetadataClient(AppId, InstanceId, messagingMock.Object);
        var act = async () => await client.FindInstancesAsync(appIdentifier);

        await act.Should().ThrowAsync<Fdc3DesktopAgentException>()
            .WithMessage("*Some error*");
    }

    [Fact]
    public async Task FindInstancesAsync_throws_when_instances_is_null()
    {
        var appIdentifier = new AppIdentifier { AppId = "app", InstanceId = "id" };
        var response = new FindInstancesResponse
        {
            Instances = null
        };

        var messagingMock = new Mock<IMessaging>();
        messagingMock
            .Setup(m => m.InvokeServiceAsync(
                Fdc3Topic.FindInstances,
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .Returns(new ValueTask<string?>(JsonSerializer.Serialize(response, _jsonSerializerOptions)));

        var client = new MetadataClient(AppId, InstanceId, messagingMock.Object);
        var act = async () => await client.FindInstancesAsync(appIdentifier);

        await act.Should().ThrowAsync<Fdc3DesktopAgentException>()
            .WithMessage("*No app matched*");
    }
}
