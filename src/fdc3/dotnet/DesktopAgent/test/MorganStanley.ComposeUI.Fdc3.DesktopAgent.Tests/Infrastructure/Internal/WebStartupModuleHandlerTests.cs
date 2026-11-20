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

using System.Diagnostics;
using System.Text;
using System.Text.Json;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.Infrastructure.Internal;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent.Shared;
using MorganStanley.ComposeUI.ModuleLoader;

namespace MorganStanley.ComposeUI.Fdc3.DesktopAgent.Tests.Infrastructure.Internal;

public class WebStartupModuleHandlerTests
{
    // Runs the generated script in an isolated Node context with a stub window and reports what it left behind.
    // Errors thrown by the rest of the script (the FDC3 bundle needs a browser) are ignored: only the startup
    // configuration at the top of the script is of interest.
    private const string NodeHarness = """
        const vm = require('vm');
        process.on('unhandledRejection', () => {});
        let script = '';
        process.stdin.setEncoding('utf8');
        process.stdin.on('data', chunk => script += chunk);
        process.stdin.on('end', () => {
            const sandbox = { window: { composeui: {} } };
            const context = vm.createContext(sandbox);
            let compileError = null;
            try {
                vm.runInContext(script, context);
            } catch (error) {
                if (error && error.name === 'SyntaxError') {
                    compileError = String(error);
                }
            }
            const fdc3 = sandbox.window.composeui.fdc3 || {};
            process.stdout.write(JSON.stringify({
                compileError,
                injectedCodeRan: vm.runInContext('typeof injected !== "undefined"', context),
                config: fdc3.config,
                channelId: fdc3.channelId,
                openAppIdentifier: fdc3.openAppIdentifier
            }));
        });
        """;

    public static TheoryData<string> HostileValues => new()
    {
        "\"};injected=true;//",
        "';injected=true;//",
        "a\"b\\c",
        "\\",
        "line1\nline2\r\nline3",
        "</script><script>injected=true</script>",
        "a" + (char)0x2028 + "b" + (char)0x2029 + "c",
        "`${injected=true}`",
        "café 😀"
    };

    [Theory]
    [MemberData(nameof(HostileValues))]
    public async Task HandleAsync_passes_the_app_id_to_the_script_as_a_literal_string(string value)
    {
        var result = await RunStartupScriptAsync(new Fdc3StartupProperties { AppId = value, InstanceId = "instance" });

        result.CompileError.Should().BeNull();
        result.InjectedCodeRan.Should().BeFalse();
        result.Config.GetProperty("appId").GetString().Should().Be(value);
    }

    [Theory]
    [MemberData(nameof(HostileValues))]
    public async Task HandleAsync_passes_the_instance_id_to_the_script_as_a_literal_string(string value)
    {
        var result = await RunStartupScriptAsync(new Fdc3StartupProperties { AppId = "app", InstanceId = value });

        result.CompileError.Should().BeNull();
        result.InjectedCodeRan.Should().BeFalse();
        result.Config.GetProperty("instanceId").GetString().Should().Be(value);
    }

    [Theory]
    [MemberData(nameof(HostileValues))]
    public async Task HandleAsync_passes_the_channel_id_to_the_script_as_a_literal_string(string value)
    {
        var result = await RunStartupScriptAsync(
            new Fdc3StartupProperties { AppId = "app", InstanceId = "instance", ChannelId = value });

        result.CompileError.Should().BeNull();
        result.InjectedCodeRan.Should().BeFalse();
        result.ChannelId.Should().Be(value);
    }

    [Theory]
    [MemberData(nameof(HostileValues))]
    public async Task HandleAsync_passes_the_opened_app_context_id_to_the_script_as_a_literal_string(string value)
    {
        var result = await RunStartupScriptAsync(
            new Fdc3StartupProperties { AppId = "app", InstanceId = "instance", OpenedAppContextId = value });

        result.CompileError.Should().BeNull();
        result.InjectedCodeRan.Should().BeFalse();
        result.OpenedAppContextId.Should().Be(value);
    }

    [Fact]
    public async Task HandleAsync_does_not_run_code_smuggled_in_through_the_app_id()
    {
        // Closes the config object, runs the assignment, then opens two blocks to swallow the closing braces of the template.
        var result = await RunStartupScriptAsync(
            new Fdc3StartupProperties { AppId = "x\"},injected:(injected=true)};{{//", InstanceId = "instance" });

        result.InjectedCodeRan.Should().BeFalse();
    }

    [Fact]
    public async Task HandleAsync_keeps_the_startup_configuration_shape_for_regular_values()
    {
        var result = await RunStartupScriptAsync(new Fdc3StartupProperties
        {
            AppId = "my-app",
            InstanceId = "6b0f5d52-6d2c-4a43-8a0e-2a4a2f0d8d11",
            ChannelId = "fdc3.channel.1",
            OpenedAppContextId = "context-1"
        });

        result.CompileError.Should().BeNull();
        result.Config.GetProperty("appId").GetString().Should().Be("my-app");
        result.Config.GetProperty("instanceId").GetString().Should().Be("6b0f5d52-6d2c-4a43-8a0e-2a4a2f0d8d11");
        result.ChannelId.Should().Be("fdc3.channel.1");
        result.OpenedAppContextId.Should().Be("context-1");
    }

    [Fact]
    public async Task HandleAsync_leaves_out_the_channel_id_and_opened_app_context_id_when_they_are_not_set()
    {
        var result = await RunStartupScriptAsync(new Fdc3StartupProperties { AppId = "my-app", InstanceId = "instance" });

        result.CompileError.Should().BeNull();
        result.ChannelId.Should().BeNull();
        result.OpenedAppContextId.Should().BeNull();
    }

    private static async Task<StartupScriptResult> RunStartupScriptAsync(Fdc3StartupProperties properties)
    {
        var moduleInstance = new Mock<IModuleInstance>();
        var startupContext = new StartupContext(new StartRequest("test"), moduleInstance.Object);

        await new WebStartupModuleHandler().HandleAsync(startupContext, properties);

        var provider = startupContext.GetOrAddProperty<WebStartupProperties>().ScriptProviders.Should().ContainSingle().Subject;
        var script = await provider(moduleInstance.Object);

        return await RunInNodeAsync(script);
    }

    private static async Task<StartupScriptResult> RunInNodeAsync(string script)
    {
        var startInfo = new ProcessStartInfo("node")
        {
            RedirectStandardInput = true,
            StandardInputEncoding = new UTF8Encoding(false),
            RedirectStandardOutput = true,
            StandardOutputEncoding = new UTF8Encoding(false),
            RedirectStandardError = true,
            UseShellExecute = false
        };
        startInfo.ArgumentList.Add("-e");
        startInfo.ArgumentList.Add(NodeHarness);

        using var process = Process.Start(startInfo)!;
        await process.StandardInput.WriteAsync(script);
        process.StandardInput.Close();

        var output = await process.StandardOutput.ReadToEndAsync();
        var error = await process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();

        process.ExitCode.Should().Be(0, error);

        using var document = JsonDocument.Parse(output);
        var root = document.RootElement;

        return new StartupScriptResult(
            root.TryGetProperty("compileError", out var compileError) && compileError.ValueKind == JsonValueKind.String
                ? compileError.GetString()
                : null,
            root.GetProperty("injectedCodeRan").GetBoolean(),
            root.TryGetProperty("config", out var config) ? config.Clone() : default,
            GetString(root, "channelId"),
            root.TryGetProperty("openAppIdentifier", out var openAppIdentifier)
                ? GetString(openAppIdentifier, "openedAppContextId")
                : null);
    }

    private static string? GetString(JsonElement element, string name) =>
        element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;

    private sealed record StartupScriptResult(
        string? CompileError,
        bool InjectedCodeRan,
        JsonElement Config,
        string? ChannelId,
        string? OpenedAppContextId);
}
