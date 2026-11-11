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

namespace MorganStanley.ComposeUI.ModuleLoader.Abstractions.Tests;

public class NativePathValidatorTests
{
    private static readonly string Root = Path.Combine(Path.GetTempPath(), "composeui-modules");

    [Theory]
    [InlineData("app.exe")]
    [InlineData("sub/app.exe")]
    [InlineData("sub/../app.exe")]
    [InlineData("./sub/./app.exe")]
    public void TryValidate_accepts_relative_paths_under_the_root_and_resolves_them_against_the_root(string relativePath)
    {
        var result = NativePathValidator.TryValidate(new Uri(relativePath, UriKind.Relative), Root, out var fullPath, out _);

        result.Should().BeTrue();
        fullPath.Should().Be(Path.GetFullPath(Path.Combine(Root, relativePath)));
    }

    [Fact]
    public void TryValidate_accepts_an_absolute_path_under_the_root()
    {
        var path = Path.Combine(Root, "sub", "app.exe");

        var result = NativePathValidator.TryValidate(new Uri(path, UriKind.Absolute), Root, out var fullPath, out _);

        result.Should().BeTrue();
        fullPath.Should().Be(path);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void TryValidate_accepts_the_root_with_or_without_a_trailing_separator(bool trailingSeparator)
    {
        var root = trailingSeparator ? Root + Path.DirectorySeparatorChar : Root;

        var result = NativePathValidator.TryValidate(new Uri("app.exe", UriKind.Relative), root, out var fullPath, out _);

        result.Should().BeTrue();
        fullPath.Should().Be(Path.Combine(Root, "app.exe"));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void TryValidate_rejects_a_sibling_directory_that_only_shares_the_root_as_a_prefix(bool trailingSeparator)
    {
        var root = trailingSeparator ? Root + Path.DirectorySeparatorChar : Root;
        var path = Path.Combine(Root + "-evil", "app.exe");

        var result = NativePathValidator.TryValidate(new Uri(path, UriKind.Absolute), root, out var fullPath, out var error);

        result.Should().BeFalse();
        fullPath.Should().BeEmpty();
        error.Should().NotBeNullOrEmpty();
    }

    [Theory]
    [InlineData("../app.exe")]
    [InlineData("sub/../../app.exe")]
    [InlineData("../composeui-modules-evil/app.exe")]
    public void TryValidate_rejects_relative_paths_that_leave_the_root(string relativePath)
    {
        var result = NativePathValidator.TryValidate(new Uri(relativePath, UriKind.Relative), Root, out _, out var error);

        result.Should().BeFalse();
        error.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void TryValidate_rejects_an_absolute_path_outside_the_root()
    {
        var path = Path.Combine(Path.GetTempPath(), "other", "app.exe");

        var result = NativePathValidator.TryValidate(new Uri(path, UriKind.Absolute), Root, out _, out _);

        result.Should().BeFalse();
    }

    [Fact]
    public void TryValidate_rejects_the_root_directory_itself()
    {
        var result = NativePathValidator.TryValidate(new Uri(Root, UriKind.Absolute), Root, out _, out _);

        result.Should().BeFalse();
    }

    [Theory]
    [InlineData("http://example.com/app.exe")]
    [InlineData("https://example.com/app.exe")]
    [InlineData("ftp://example.com/app.exe")]
    public void TryValidate_rejects_uris_that_are_not_files(string uri)
    {
        var result = NativePathValidator.TryValidate(new Uri(uri, UriKind.Absolute), Root, out _, out var error);

        result.Should().BeFalse();
        error.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void TryValidate_rejects_a_null_path()
    {
        var result = NativePathValidator.TryValidate(null, Root, out _, out _);

        result.Should().BeFalse();
    }

    [Fact]
    public void TryValidate_throws_when_the_root_is_empty()
    {
        var action = () => NativePathValidator.TryValidate(new Uri("app.exe", UriKind.Relative), "", out _, out _);

        action.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData("file:////?/C:/composeui-modules/app.exe")]
    [InlineData("file://?/C:/composeui-modules/app.exe")]
    public void TryValidate_rejects_file_uris_that_try_to_pass_a_device_path(string uri)
    {
        var result = NativePathValidator.TryValidate(new Uri(uri, UriKind.Absolute), Root, out _, out _);

        result.Should().BeFalse();
    }

    [Fact]
    public void TryValidate_rejects_a_unc_path_when_the_root_is_not_a_unc_path()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        var result = NativePathValidator.TryValidate(new Uri(@"\\server\share\app.exe", UriKind.Absolute), Root, out _, out _);

        result.Should().BeFalse();
    }

    [Fact]
    public void TryValidate_accepts_a_unc_path_under_a_unc_root()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        var result = NativePathValidator.TryValidate(
            new Uri(@"\\server\share\modules\app.exe", UriKind.Absolute),
            @"\\server\share\modules",
            out var fullPath,
            out _);

        result.Should().BeTrue();
        fullPath.Should().Be(@"\\server\share\modules\app.exe");
    }
}
