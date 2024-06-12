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

using Microsoft.Extensions.Hosting;
using Moq;
using MorganStanley.ComposeUI.Messaging;

namespace MorganStanley.ComposeUI.Shell.Fdc3.ResolverUI;

public class Fdc3ResolverUIServiceTests
{
    [Fact]
    public async Task StartAsync_registers_MessageRouter_service()
    {
        var resolverUiWindowMock = new Mock<IResolverUIProjector>();
        var messageRouterMock = new Mock<IMessageRouter>();
        var serviceProviderMock = new Mock<IServiceProvider>();
        var hostMock = new Mock<IHost>();

        hostMock.Setup(_ => _.Services)
            .Returns(serviceProviderMock.Object);

        serviceProviderMock.Setup(
            _ => _.GetService(typeof(IMessageRouter)))
            .Returns(messageRouterMock.Object);

        var fdc3ResolverUiService = new Fdc3ResolverUIService(
            hostMock.Object,
            resolverUiWindowMock.Object);

        await fdc3ResolverUiService.StartAsync(CancellationToken.None);

        messageRouterMock.Verify(
            _ => _.RegisterServiceAsync("ComposeUI/fdc3/v2.0/resolverUI", It.IsAny<MessageHandler>(), It.IsAny<EndpointDescriptor>(), It.IsAny<CancellationToken>()));
    }
}
