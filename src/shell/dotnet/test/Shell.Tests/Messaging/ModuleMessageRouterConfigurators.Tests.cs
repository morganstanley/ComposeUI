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
using MorganStanley.ComposeUI.Messaging.Server.WebSocket;
using MorganStanley.ComposeUI.ModuleLoader;

namespace MorganStanley.ComposeUI.Shell.Messaging;

public class ModuleMessageRouterConfiguratorsTests
{
    private readonly ModuleTokenStore _store = new();
    private readonly Mock<IMessageRouterWebSocketServer> _webSocketServer = new();

    public ModuleMessageRouterConfiguratorsTests()
    {
        _webSocketServer.SetupGet(_ => _.WebSocketUrl).Returns(new Uri("ws://localhost:1234/"));
    }

    [Fact]
    public async Task The_web_configurator_injects_the_token_issued_to_the_module_instance()
    {
        var instanceId = Guid.NewGuid();
        var context = CreateContext(instanceId, "module-a");

        new WebModuleMessageRouterConfigurator(_store).Configure(context, _webSocketServer.Object);

        var script = await GetWebScript(context);
        var token = ExtractWebToken(script);
        _store.TryValidate(token, out var identity).Should().BeTrue();
        identity.Should().Be(new ModuleIdentity(instanceId, "module-a"));
        script.Should().Contain("ws://localhost:1234/");
    }

    [Fact]
    public async Task The_web_configurator_injects_different_tokens_into_different_modules()
    {
        var contextA = CreateContext(Guid.NewGuid(), "module-a");
        var contextB = CreateContext(Guid.NewGuid(), "module-b");
        var configurator = new WebModuleMessageRouterConfigurator(_store);

        configurator.Configure(contextA, _webSocketServer.Object);
        configurator.Configure(contextB, _webSocketServer.Object);

        ExtractWebToken(await GetWebScript(contextA)).Should().NotBe(ExtractWebToken(await GetWebScript(contextB)));
    }

    [Fact]
    public async Task The_web_configurator_does_not_inject_the_host_token()
    {
        var context = CreateContext(Guid.NewGuid(), "module-a");

        new WebModuleMessageRouterConfigurator(_store).Configure(context, _webSocketServer.Object);

        (await GetWebScript(context)).Should().NotContain(_store.HostToken);
    }

    [Fact]
    public void The_native_configurator_passes_the_token_issued_to_the_module_instance()
    {
        var instanceId = Guid.NewGuid();
        var context = CreateContext(instanceId, "module-a");

        new NativeModuleMessageRouterConfigurator(_store).Configure(context, _webSocketServer.Object);

        var token = GetNativeToken(context);
        _store.TryValidate(token, out var identity).Should().BeTrue();
        identity.Should().Be(new ModuleIdentity(instanceId, "module-a"));
    }

    [Fact]
    public void The_native_configurator_passes_different_tokens_to_different_modules()
    {
        var contextA = CreateContext(Guid.NewGuid(), "module-a");
        var contextB = CreateContext(Guid.NewGuid(), "module-b");
        var configurator = new NativeModuleMessageRouterConfigurator(_store);

        configurator.Configure(contextA, _webSocketServer.Object);
        configurator.Configure(contextB, _webSocketServer.Object);

        GetNativeToken(contextA).Should().NotBe(GetNativeToken(contextB));
    }

    [Fact]
    public void The_native_configurator_does_not_pass_the_host_token()
    {
        var context = CreateContext(Guid.NewGuid(), "module-a");

        new NativeModuleMessageRouterConfigurator(_store).Configure(context, _webSocketServer.Object);

        GetNativeToken(context).Should().NotBe(_store.HostToken);
    }

    [Fact]
    public void The_startup_action_configures_modules_by_type()
    {
        var webContext = CreateContext(Guid.NewGuid(), "web-module", ModuleType.Web);
        var nativeContext = CreateContext(Guid.NewGuid(), "native-module", ModuleType.Native);
        var action = new MessageRouterStartupAction(_store, _webSocketServer.Object);

        action.InvokeAsync(webContext, () => Task.CompletedTask).Wait();
        action.InvokeAsync(nativeContext, () => Task.CompletedTask).Wait();

        webContext.GetProperties<WebStartupProperties>().Should().ContainSingle();
        nativeContext.GetProperties<EnvironmentVariables>().Should().ContainSingle();
        _store.TryValidate(GetNativeToken(nativeContext), out var identity).Should().BeTrue();
        identity!.ModuleId.Should().Be("native-module");
    }

    private static StartupContext CreateContext(Guid instanceId, string moduleId, string moduleType = ModuleType.Web)
    {
        var manifest = new Mock<IModuleManifest>();
        manifest.SetupGet(_ => _.Id).Returns(moduleId);
        manifest.SetupGet(_ => _.ModuleType).Returns(moduleType);

        var instance = new Mock<IModuleInstance>();
        instance.SetupGet(_ => _.InstanceId).Returns(instanceId);
        instance.SetupGet(_ => _.Manifest).Returns(manifest.Object);

        return new StartupContext(new StartRequest(moduleId), instance.Object);
    }

    private static async Task<string> GetWebScript(StartupContext context)
    {
        var provider = context.GetProperties<WebStartupProperties>().Single().ScriptProviders.Single();

        return await provider(context.ModuleInstance);
    }

    private static string ExtractWebToken(string script)
    {
        var match = System.Text.RegularExpressions.Regex.Match(script, "accessToken: \"([^\"]+)\"");
        match.Success.Should().BeTrue();

        return match.Groups[1].Value;
    }

    private static string GetNativeToken(StartupContext context)
    {
        return context.GetProperties<EnvironmentVariables>()
            .Single()
            .Variables
            .Single(v => v.Key == ComposeUI.Messaging.EnvironmentVariableNames.AccessToken)
            .Value;
    }
}
