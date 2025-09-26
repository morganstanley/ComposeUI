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
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.Client.Infrastructure.Internal;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.Shared;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.Shared.Contracts;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.Shared.Exceptions;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.Shared.Protocol;
using MorganStanley.ComposeUI.Messaging.Abstractions;

namespace MorganStanley.ComposeUI.Fdc3.DesktopAgent.Client.Tests.Infrastructure.Internal;

public class IntentsClientTests
{
    private readonly JsonSerializerOptions _jsonSerializerOptions = SerializerOptionsHelper.JsonSerializerOptionsWithContextSerialization;
    private readonly Mock<IMessaging> _messagingMock = new();
    private readonly IntentsClient _client;

    public IntentsClientTests()
    {
        _client = new IntentsClient(_messagingMock.Object, "testInstance");
    }

    [Fact]
    public async Task FindIntentAsync_returns_AppIntent_when_response_is_successful()
    {
        var expectedAppIntent = new AppIntent
        {
            Intent = new IntentMetadata
            {
                Name = "IntentName",
                DisplayName = "Intent Display Name"
            },
            Apps = new[]
            {
                new AppMetadata
                {
                    AppId = "AppId",
                    Name = "App Name",
                    Version = "1.0.0"
                }
            }
        };
        var response = new FindIntentResponse
        {
            AppIntent = expectedAppIntent
        };

        _messagingMock
            .Setup(m => m.InvokeServiceAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(JsonSerializer.Serialize(response, _jsonSerializerOptions));

        var result = await _client.FindIntentAsync("IntentName");

        result.Should().BeEquivalentTo(expectedAppIntent);
    }

    [Fact]
    public async Task FindIntentAsync_throws_MissingResponse_when_response_is_null()
    {
        _messagingMock
            .Setup(m => m.InvokeServiceAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?) null);

        var act = async () => await _client.FindIntentAsync("IntentName");

        await act.Should().ThrowAsync<Fdc3DesktopAgentException>()
            .WithMessage("*No response was received from the FDC3 backend server*");
    }

    [Fact]
    public async Task FindIntentAsync_throws_ErrorResponseReceived_when_response_has_error()
    {
        var response = new FindIntentResponse
        {
            Error = "Some error"
        };

        _messagingMock
            .Setup(m => m.InvokeServiceAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(JsonSerializer.Serialize(response, _jsonSerializerOptions));

        var act = async () => await _client.FindIntentAsync("IntentName");

        await act.Should().ThrowAsync<Fdc3DesktopAgentException>()
            .WithMessage("*Some error*");
    }

    [Fact]
    public async Task FindIntentAsync_throws_AppIntentIsNotDefined_when_AppIntent_is_null()
    {
        var response = new FindIntentResponse
        {
            AppIntent = null
        };

        _messagingMock
            .Setup(m => m.InvokeServiceAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(JsonSerializer.Serialize(response, _jsonSerializerOptions));

        var act = async () => await _client.FindIntentAsync("IntentName");

        await act.Should().ThrowAsync<Fdc3DesktopAgentException>()
            .WithMessage("*The AppIntent was not returned by the FDC3 DesktopAgent backend for intent: IntentName; context: ; and resultType: .*");
    }
}