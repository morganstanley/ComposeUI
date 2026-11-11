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

using System.Diagnostics;
using FluentAssertions;
using MorganStanley.ComposeUI.ModuleLoader.Runners;

namespace MorganStanley.ComposeUI.ModuleLoader.Tests.Runners;

public class NativeLaunchGuardTests : IDisposable
{
    private readonly string _base = Path.Combine(Path.GetTempPath(), "composeui-guard-" + Guid.NewGuid().ToString("N"));
    private readonly string _root;
    private readonly string _outside;
    private readonly List<string> _links = new();

    public NativeLaunchGuardTests()
    {
        _root = Path.Combine(_base, "modules");
        _outside = Path.Combine(_base, "outside");
        Directory.CreateDirectory(Path.Combine(_root, "sub"));
        Directory.CreateDirectory(_outside);
        File.WriteAllText(Path.Combine(_root, "app.exe"), "");
        File.WriteAllText(Path.Combine(_root, "sub", "other.exe"), "");
        File.WriteAllText(Path.Combine(_outside, "evil.exe"), "");
    }

    public void Dispose()
    {
        foreach (var link in _links.Where(Directory.Exists))
        {
            Directory.Delete(link, recursive: false);
        }

        Directory.Delete(_base, recursive: true);
    }

    [Fact]
    public void Validate_returns_the_full_path_of_a_module_under_an_allowed_root()
    {
        var path = NativeLaunchGuard.Validate("app", Details(Path.Combine(_root, "sub", "other.exe")), Options());

        path.Should().Be(Path.Combine(_root, "sub", "other.exe"));
    }

    [Fact]
    public void Validate_resolves_a_relative_path_against_the_allowed_root()
    {
        var details = new NativeManifestDetails { Path = new Uri(Path.Combine("sub", "other.exe"), UriKind.Relative) };

        var path = NativeLaunchGuard.Validate("app", details, Options());

        path.Should().Be(Path.Combine(_root, "sub", "other.exe"));
    }

    [Fact]
    public void Validate_accepts_a_module_under_any_of_the_allowed_roots()
    {
        var options = Options();
        options.AllowedRoots.Insert(0, Path.Combine(_base, "unused"));

        var path = NativeLaunchGuard.Validate("app", Details(Path.Combine(_root, "app.exe")), options);

        path.Should().Be(Path.Combine(_root, "app.exe"));
    }

    [Fact]
    public void Validate_rejects_a_module_outside_the_allowed_roots_and_names_the_module()
    {
        var action = () => NativeLaunchGuard.Validate("evilApp", Details(Path.Combine(_outside, "evil.exe")), Options());

        action.Should().Throw<ModuleLoaderException>().WithMessage("*evilApp*");
    }

    [Fact]
    public void Validate_uses_the_default_root_when_no_root_is_configured()
    {
        var previous = Environment.GetEnvironmentVariable(NativeModuleLaunchOptions.RepositoryRootEnvironmentVariableName);
        Environment.SetEnvironmentVariable(NativeModuleLaunchOptions.RepositoryRootEnvironmentVariableName, _root);

        try
        {
            var path = NativeLaunchGuard.Validate("app", Details(Path.Combine(_root, "app.exe")), new NativeModuleLaunchOptions());

            path.Should().Be(Path.Combine(_root, "app.exe"));
        }
        finally
        {
            Environment.SetEnvironmentVariable(NativeModuleLaunchOptions.RepositoryRootEnvironmentVariableName, previous);
        }
    }

    [Fact]
    public void Validate_rejects_a_path_through_a_junction_that_points_outside_the_root()
    {
        if (!TryCreateJunction(Path.Combine(_root, "link"), _outside))
        {
            return;
        }

        var action = () => NativeLaunchGuard.Validate("app", Details(Path.Combine(_root, "link", "evil.exe")), Options());

        action.Should().Throw<ModuleLoaderException>().WithMessage("*app*");
    }

    [Fact]
    public void Validate_accepts_a_path_through_a_junction_that_points_inside_the_root()
    {
        if (!TryCreateJunction(Path.Combine(_root, "link"), Path.Combine(_root, "sub")))
        {
            return;
        }

        var path = NativeLaunchGuard.Validate("app", Details(Path.Combine(_root, "link", "other.exe")), Options());

        path.Should().Be(Path.Combine(_root, "sub", "other.exe"));
    }

    [Fact]
    public void Validate_rejects_a_file_symbolic_link_that_points_outside_the_root()
    {
        var link = Path.Combine(_root, "evil-link.exe");

        try
        {
            File.CreateSymbolicLink(link, Path.Combine(_outside, "evil.exe"));
        }
        catch (Exception e) when (e is UnauthorizedAccessException or IOException)
        {
            // Creating symbolic links needs a privilege on Windows.
            return;
        }

        var action = () => NativeLaunchGuard.Validate("app", Details(link), Options());

        action.Should().Throw<ModuleLoaderException>();
    }

    [Fact]
    public void Validate_accepts_any_executable_when_no_executable_list_is_configured()
    {
        var action = () => NativeLaunchGuard.Validate("app", Details(Path.Combine(_root, "sub", "other.exe")), Options());

        action.Should().NotThrow();
    }

    [Fact]
    public void Validate_accepts_an_executable_that_is_on_the_executable_list_by_file_name()
    {
        var options = Options();
        options.AllowedExecutables.Add("OTHER.exe");

        var action = () => NativeLaunchGuard.Validate("app", Details(Path.Combine(_root, "sub", "other.exe")), options);

        action.Should().NotThrow();
    }

    [Fact]
    public void Validate_accepts_an_executable_that_is_on_the_executable_list_by_full_path()
    {
        var options = Options();
        options.AllowedExecutables.Add(Path.Combine(_root, "app.exe"));

        var action = () => NativeLaunchGuard.Validate("app", Details(Path.Combine(_root, "app.exe")), options);

        action.Should().NotThrow();
    }

    [Fact]
    public void Validate_rejects_an_executable_that_is_not_on_the_executable_list()
    {
        var options = Options();
        options.AllowedExecutables.Add("app.exe");

        var action = () => NativeLaunchGuard.Validate("app", Details(Path.Combine(_root, "sub", "other.exe")), options);

        action.Should().Throw<ModuleLoaderException>();
    }

    [Fact]
    public void Validate_accepts_arguments_within_the_limits()
    {
        var options = Options();
        options.MaxArgumentCount = 2;
        options.MaxArgumentLength = 5;
        var details = Details(Path.Combine(_root, "app.exe"), arguments: ["12345", "abc"]);

        var action = () => NativeLaunchGuard.Validate("app", details, options);

        action.Should().NotThrow();
    }

    [Fact]
    public void Validate_rejects_too_many_arguments()
    {
        var options = Options();
        options.MaxArgumentCount = 2;
        var details = Details(Path.Combine(_root, "app.exe"), arguments: ["a", "b", "c"]);

        var action = () => NativeLaunchGuard.Validate("app", details, options);

        action.Should().Throw<ModuleLoaderException>();
    }

    [Fact]
    public void Validate_rejects_an_argument_that_is_too_long()
    {
        var options = Options();
        options.MaxArgumentLength = 5;
        var details = Details(Path.Combine(_root, "app.exe"), arguments: ["123456"]);

        var action = () => NativeLaunchGuard.Validate("app", details, options);

        action.Should().Throw<ModuleLoaderException>();
    }

    [Fact]
    public void Validate_rejects_an_argument_with_a_nul_character()
    {
        var details = Details(Path.Combine(_root, "app.exe"), arguments: ["a\0b"]);

        var action = () => NativeLaunchGuard.Validate("app", details, Options());

        action.Should().Throw<ModuleLoaderException>();
    }

    [Theory]
    [InlineData("PATH")]
    [InlineData("path")]
    [InlineData("PATHEXT")]
    [InlineData("COMSPEC")]
    [InlineData("DOTNET_STARTUP_HOOKS")]
    [InlineData("dotnet_gcheapcount")]
    [InlineData("COR_PROFILER")]
    [InlineData("CORECLR_ENABLE_PROFILING")]
    [InlineData("LD_PRELOAD")]
    [InlineData("LD_LIBRARY_PATH")]
    [InlineData("DYLD_INSERT_LIBRARIES")]
    public void Validate_rejects_a_blocked_environment_variable(string name)
    {
        var details = Details(Path.Combine(_root, "app.exe"), variables: new() { [name] = "value" });

        var action = () => NativeLaunchGuard.Validate("app", details, Options());

        action.Should().Throw<ModuleLoaderException>().WithMessage($"*{name}*");
    }

    [Theory]
    [InlineData("MY_VARIABLE")]
    [InlineData("COMPOSEUI_TEST")]
    [InlineData("PATHS_ARE_FINE")]
    public void Validate_accepts_an_environment_variable_that_is_not_blocked(string name)
    {
        var details = Details(Path.Combine(_root, "app.exe"), variables: new() { [name] = "value" });

        var action = () => NativeLaunchGuard.Validate("app", details, Options());

        action.Should().NotThrow();
    }

    [Theory]
    [InlineData("A=B")]
    [InlineData("A\0B")]
    [InlineData("")]
    public void Validate_rejects_an_invalid_environment_variable_name(string name)
    {
        var details = Details(Path.Combine(_root, "app.exe"), variables: new() { [name] = "value" });

        var action = () => NativeLaunchGuard.Validate("app", details, Options());

        action.Should().Throw<ModuleLoaderException>();
    }

    [Fact]
    public void Validate_uses_the_configured_list_of_blocked_environment_variables()
    {
        var options = Options();
        options.BlockedEnvironmentVariables.Clear();
        options.BlockedEnvironmentVariables.Add("SECRET_*");

        var blocked = Details(Path.Combine(_root, "app.exe"), variables: new() { ["SECRET_KEY"] = "x" });
        var allowed = Details(Path.Combine(_root, "app.exe"), variables: new() { ["PATH"] = "x" });

        ((Action) (() => NativeLaunchGuard.Validate("app", blocked, options))).Should().Throw<ModuleLoaderException>();
        ((Action) (() => NativeLaunchGuard.Validate("app", allowed, options))).Should().NotThrow();
    }

    private NativeModuleLaunchOptions Options()
    {
        var options = new NativeModuleLaunchOptions();
        options.AllowedRoots.Add(_root);
        return options;
    }

    private static NativeManifestDetails Details(
        string path,
        string[]? arguments = null,
        Dictionary<string, string>? variables = null) =>
        new()
        {
            Path = new Uri(path, UriKind.Absolute),
            Arguments = arguments ?? [],
            EnvironmentVariables = variables ?? new()
        };

    private bool TryCreateJunction(string link, string target)
    {
        if (!OperatingSystem.IsWindows())
        {
            return false;
        }

        using var process = Process.Start(new ProcessStartInfo("cmd.exe", $"/c mklink /J \"{link}\" \"{target}\"")
        {
            CreateNoWindow = true,
            UseShellExecute = false,
            RedirectStandardOutput = true
        })!;
        process.WaitForExit();

        if (process.ExitCode != 0)
        {
            return false;
        }

        _links.Add(link);
        return true;
    }
}
