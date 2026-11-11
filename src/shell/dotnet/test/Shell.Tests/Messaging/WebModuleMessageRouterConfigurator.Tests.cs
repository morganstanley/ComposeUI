// Morgan Stanley makes this available to you under the Apache License,
// Version 2.0 (the "License"). You may obtain a copy of the License at
// 
//      http://www.apache.org/licenses/LICENSE-2.0.
// 
// See the NOTICE file distributed with this work for additional information
// regarding copyright ownership. Unless required by applicable law or agreed
// to in writing, software distributed under the License is distributed on an
// "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express
// or implied. See the License for the specific language governing permissions
// and limitations under the License.

using FluentAssertions;
using Moq;
using MorganStanley.ComposeUI.Messaging.Server.WebSocket;
using MorganStanley.ComposeUI.ModuleLoader;

namespace MorganStanley.ComposeUI.Shell.Messaging;

public class WebModuleMessageRouterConfiguratorTests
{
    [Fact]
    public void The_script_contains_the_access_token_and_the_escaped_WebSocket_url()
    {
        string? script = null;

        var statThread = new Thread(
            () =>
            {
                var app = new App();
                var webSocketServer = new Mock<IMessageRouterWebSocketServer>();
                // Uri.ToString() would turn %22 back into a quote and break out of the JavaScript string
                webSocketServer.SetupGet(_ => _.WebSocketUrl).Returns(new Uri("ws://localhost:1234/a%22b"));
                var moduleInstance = Mock.Of<IModuleInstance>();
                var startupContext = new StartupContext(new StartRequest("test"), moduleInstance);

                new WebModuleMessageRouterConfigurator().Configure(startupContext, webSocketServer.Object);

                var scriptProvider = startupContext.GetOrAddProperty<WebStartupProperties>().ScriptProviders.Single();
                script = scriptProvider(moduleInstance).AsTask().GetAwaiter().GetResult();
                script = script.Replace(app.MessageRouterAccessToken, "<token>");
            });

        statThread.SetApartmentState(ApartmentState.STA);
        statThread.Start();
        statThread.Join();

        script.Should().Contain("accessToken: \"<token>\"");
        script.Should().Contain("url: \"ws://localhost:1234/a%22b\"");
    }
}
