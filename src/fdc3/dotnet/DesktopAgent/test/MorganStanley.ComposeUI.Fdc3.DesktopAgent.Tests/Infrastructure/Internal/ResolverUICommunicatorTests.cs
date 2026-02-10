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
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.Infrastructure.Internal;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.Shared.Contracts;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.Shared.Converters;
using MorganStanley.ComposeUI.Messaging.Abstractions;
using AppMetadata = MorganStanley.ComposeUI.Fdc3.DesktopAgent.Shared.Protocol.AppMetadata;

namespace MorganStanley.ComposeUI.Fdc3.DesktopAgent.Tests.Infrastructure.Internal;

public class ResolverUICommunicatorTests
{
    private readonly JsonSerializerOptions _jsonSerializerOptions = new()
    {
        Converters = { new AppMetadataJsonConverter() }
    };

    [Fact]
    public async Task SendResolverUIRequest_will_return_null()
    {
        var messagingMock = new Mock<IMessaging>();
        messagingMock.Setup(
                _ => _.InvokeServiceAsync(
                    It.IsAny<string>(),
                    It.IsAny<string?>(),
                    It.IsAny<CancellationToken>()))
            .Returns(null);

        var resolverUICommunicator = new ResolverUICommunicator(messagingMock.Object, null);

        var response = await resolverUICommunicator.SendResolverUIRequestAsync(It.IsAny<IEnumerable<IAppMetadata>>());

        response.Should().BeNull();
    }

    [Fact]
    public async Task SendResolverUIRequest_will_return_response()
    {
        var messagingMock = new Mock<IMessaging>();
        messagingMock.Setup(
                _ => _.InvokeServiceAsync(
                    It.IsAny<string>(),
                    It.IsAny<string?>(),
                    It.IsAny<CancellationToken>()))
            .Returns(ValueTask.FromResult<string?>(
                JsonSerializer.Serialize(new ResolverUIResponse()
                {
                    AppMetadata = new AppMetadata() { AppId = "testAppId" }
                }, _jsonSerializerOptions)));

        var resolverUICommunicator = new ResolverUICommunicator(messagingMock.Object, null);

        var response = await resolverUICommunicator.SendResolverUIRequestAsync(It.IsAny<IEnumerable<IAppMetadata>>());

        response.Should().NotBeNull();
        response!.AppMetadata.Should().NotBeNull();
        response.AppMetadata!.AppId.Should().Be("testAppId");
    }
}