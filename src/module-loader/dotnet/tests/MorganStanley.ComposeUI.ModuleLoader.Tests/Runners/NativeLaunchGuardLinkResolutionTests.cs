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
using MorganStanley.ComposeUI.ModuleLoader.Runners;

namespace MorganStanley.ComposeUI.ModuleLoader.Tests.Runners;

/// <summary>
/// The links are faked, so these tests do not depend on what the operating system lets a process create.
/// A fake link gives the text that the link stores, like <see cref="FileSystemInfo.LinkTarget"/> does on Unix.
/// </summary>
public class NativeLaunchGuardLinkResolutionTests
{
    private static readonly string Base = Path.Combine(Path.GetTempPath(), "composeui-links");

    private static string P(params string[] parts) => Path.Combine([Base, .. parts]);

    private static Func<string, string?> Links(Dictionary<string, string> links) => path => links.GetValueOrDefault(path);

    [Fact]
    public void ResolveLinks_returns_a_path_without_links_as_it_is()
    {
        var resolved = NativeLaunchGuard.ResolveLinks(P("root", "sub", "app.exe"), Links([]));

        resolved.Should().Be(P("root", "sub", "app.exe"));
    }

    [Fact]
    public void ResolveLinks_resolves_a_link_in_the_middle_of_the_path()
    {
        var links = new Dictionary<string, string> { [P("root", "a")] = P("outside") };

        var resolved = NativeLaunchGuard.ResolveLinks(P("root", "a", "app.exe"), Links(links));

        resolved.Should().Be(P("outside", "app.exe"));
    }

    [Fact]
    public void ResolveLinks_resolves_a_link_that_is_the_last_component()
    {
        var links = new Dictionary<string, string> { [P("root", "app.exe")] = P("outside", "evil.exe") };

        var resolved = NativeLaunchGuard.ResolveLinks(P("root", "app.exe"), Links(links));

        resolved.Should().Be(P("outside", "evil.exe"));
    }

    [Fact]
    public void ResolveLinks_resolves_a_link_whose_target_runs_through_another_link()
    {
        // root/a -> root/b/c, and root/b -> outside, so root/a is outside/c.
        var links = new Dictionary<string, string>
        {
            [P("root", "a")] = P("root", "b", "c"),
            [P("root", "b")] = P("outside")
        };

        var resolved = NativeLaunchGuard.ResolveLinks(P("root", "a", "app.exe"), Links(links));

        resolved.Should().Be(P("outside", "c", "app.exe"));
    }

    [Fact]
    public void ResolveLinks_resolves_a_relative_target_from_the_directory_of_the_link()
    {
        var links = new Dictionary<string, string> { [P("root", "a")] = "sub" };

        var resolved = NativeLaunchGuard.ResolveLinks(P("root", "a", "app.exe"), Links(links));

        resolved.Should().Be(P("root", "sub", "app.exe"));
    }

    [Fact]
    public void ResolveLinks_resolves_dot_dot_in_a_target_after_the_links_that_come_before_it()
    {
        // root/a -> "b/../c", and root/b -> outside/y. The parent of root/b is outside, not root, so root/a is outside/c.
        var links = new Dictionary<string, string>
        {
            [P("root", "a")] = Path.Combine("b", "..", "c"),
            [P("root", "b")] = P("outside", "y")
        };

        var resolved = NativeLaunchGuard.ResolveLinks(P("root", "a", "app.exe"), Links(links));

        resolved.Should().Be(P("outside", "c", "app.exe"));
    }

    [Fact]
    public void ResolveLinks_ignores_a_single_dot_in_a_target()
    {
        var links = new Dictionary<string, string> { [P("root", "a")] = Path.Combine(".", "sub", ".", "deep") };

        var resolved = NativeLaunchGuard.ResolveLinks(P("root", "a", "app.exe"), Links(links));

        resolved.Should().Be(P("root", "sub", "deep", "app.exe"));
    }

    [Fact]
    public void ResolveLinks_stays_at_the_root_when_a_target_goes_above_it()
    {
        var links = new Dictionary<string, string> { [P("root", "a")] = Path.Combine(Enumerable.Repeat("..", 60).ToArray()) };

        var resolved = NativeLaunchGuard.ResolveLinks(P("root", "a", "app.exe"), Links(links));

        resolved.Should().Be(Path.Combine(Path.GetPathRoot(Base)!, "app.exe"));
    }

    [Fact]
    public void ResolveLinks_throws_when_links_point_to_each_other()
    {
        var links = new Dictionary<string, string>
        {
            [P("root", "a")] = P("root", "b"),
            [P("root", "b")] = P("root", "a")
        };

        var action = () => NativeLaunchGuard.ResolveLinks(P("root", "a", "app.exe"), Links(links));

        action.Should().Throw<IOException>();
    }
}
