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

using Finos.Fdc3.AppDirectory;
using MorganStanley.ComposeUI.ModuleLoader;
using Newtonsoft.Json;

namespace MorganStanley.ComposeUI.Fdc3.AppDirectory;

[CollectionDefinition(nameof(ModuleRepositoryRootCollection), DisableParallelization = true)]
public class ModuleRepositoryRootCollection;

/// <summary>
/// The module repository root is read from a process-wide environment variable, so these tests must not run in parallel.
/// </summary>
[Collection(nameof(ModuleRepositoryRootCollection))]
public class Fdc3ModuleCatalogNativeModulesTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "composeui-catalog-" + Guid.NewGuid().ToString("N"));
    private readonly string? _previousRoot;

    public Fdc3ModuleCatalogNativeModulesTests()
    {
        _previousRoot = Environment.GetEnvironmentVariable(Consts.COMPOSEUI_MODULE_REPOSITORY_ENVIRONMENT_VARIABLE_NAME);
        Environment.SetEnvironmentVariable(Consts.COMPOSEUI_MODULE_REPOSITORY_ENVIRONMENT_VARIABLE_NAME, _root);
    }

    public void Dispose()
    {
        Environment.SetEnvironmentVariable(Consts.COMPOSEUI_MODULE_REPOSITORY_ENVIRONMENT_VARIABLE_NAME, _previousRoot);
    }

    [Fact]
    public async Task GetManifest_returns_native_manifest_for_an_absolute_path_under_the_root()
    {
        var path = Path.Combine(_root, "sub", "app.exe");
        var catalog = CreateCatalog(path);

        var manifest = await catalog.GetManifest("nativeApp");

        GetDetails(manifest).Path.LocalPath.Should().Be(path);
    }

    [Fact]
    public async Task GetManifest_uses_the_modules_directory_of_the_executable_when_no_root_is_configured()
    {
        Environment.SetEnvironmentVariable(Consts.COMPOSEUI_MODULE_REPOSITORY_ENVIRONMENT_VARIABLE_NAME, null);
        var modulesDirectory = Path.Combine(AppContext.BaseDirectory, "modules");

        var inModules = await CreateCatalog(Path.Combine("sub", "app.exe")).GetManifest("nativeApp");
        var outside = () => CreateCatalog(Path.Combine(AppContext.BaseDirectory, "app.exe")).GetManifest("nativeApp");

        GetDetails(inModules).Path.LocalPath.Should().Be(Path.Combine(modulesDirectory, "sub", "app.exe"));
        await outside.Should().ThrowAsync<NotSupportedException>();
    }

    [Fact]
    public async Task GetManifest_resolves_a_relative_path_against_the_root_and_not_against_the_working_directory()
    {
        var catalog = CreateCatalog(Path.Combine("sub", "app.exe"));

        var manifest = await catalog.GetManifest("nativeApp");

        var details = GetDetails(manifest);
        details.Path.IsAbsoluteUri.Should().BeTrue();
        details.Path.LocalPath.Should().Be(Path.Combine(_root, "sub", "app.exe"));
    }

    [Fact]
    public async Task GetManifest_throws_for_a_directory_that_only_shares_the_root_as_a_prefix()
    {
        var catalog = CreateCatalog(Path.Combine(_root + "-evil", "app.exe"));

        var action = () => catalog.GetManifest("nativeApp");

        await action.Should().ThrowAsync<NotSupportedException>().WithMessage("*nativeApp*");
    }

    [Fact]
    public async Task GetManifest_throws_for_a_relative_path_that_leaves_the_root()
    {
        var catalog = CreateCatalog(Path.Combine("..", "outside", "app.exe"));

        var action = () => catalog.GetManifest("nativeApp");

        await action.Should().ThrowAsync<NotSupportedException>().WithMessage("*nativeApp*");
    }

    [Fact]
    public async Task GetManifest_throws_for_an_http_path()
    {
        var catalog = CreateCatalog("http://example.com/app.exe");

        var action = () => catalog.GetManifest("nativeApp");

        await action.Should().ThrowAsync<NotSupportedException>().WithMessage("*nativeApp*");
    }

    [Theory]
    [InlineData(@"\\?\C:\apps\app.exe")]
    [InlineData(@"\\.\C:\apps\app.exe")]
    public async Task GetManifest_throws_with_the_name_of_the_app_when_the_path_is_not_a_valid_uri(string path)
    {
        var catalog = CreateCatalog(path);

        var action = () => catalog.GetManifest("nativeApp");

        await action.Should().ThrowAsync<NotSupportedException>().WithMessage("*nativeApp*");
    }

    [Fact]
    public async Task GetManifest_throws_with_the_name_of_the_app_when_the_host_manifest_mapper_cannot_build_a_uri()
    {
        var mapper = new Mock<IHostManifestMapper>();
        mapper.Setup(_ => _.MapModuleDetails(It.IsAny<Fdc3App>())).Throws(new UriFormatException("Invalid URI"));
        var catalog = CreateCatalog(Path.Combine(_root, "app.exe"), mapper.Object);

        var action = () => catalog.GetManifest("nativeApp");

        await action.Should().ThrowAsync<NotSupportedException>().WithMessage("*nativeApp*");
    }

    [Fact]
    public async Task GetManifest_throws_when_the_host_manifest_mapper_returns_a_path_outside_the_root()
    {
        var mapper = CreateMapper(new NativeManifestDetails { Path = new Uri(Path.Combine(Path.GetTempPath(), "outside", "app.exe")) });
        var catalog = CreateCatalog(Path.Combine(_root, "app.exe"), mapper);

        var action = () => catalog.GetManifest("nativeApp");

        await action.Should().ThrowAsync<NotSupportedException>().WithMessage("*nativeApp*");
    }

    [Fact]
    public async Task GetManifest_returns_the_details_of_the_host_manifest_mapper_with_the_full_path_when_it_is_under_the_root()
    {
        var mapper = CreateMapper(new NativeManifestDetails
        {
            Path = new Uri(Path.Combine("sub", "app.exe"), UriKind.Relative),
            Arguments = ["--flag"]
        });
        var catalog = CreateCatalog(Path.Combine(_root, "app.exe"), mapper, new AppDirectoryOptions { AllowNativeArguments = true });

        var manifest = await catalog.GetManifest("nativeApp");

        var details = GetDetails(manifest);
        details.Path.LocalPath.Should().Be(Path.Combine(_root, "sub", "app.exe"));
        details.Arguments.Should().Equal("--flag");
    }

    [Fact]
    public async Task GetManifest_throws_when_the_app_directory_gives_arguments_and_they_are_not_allowed()
    {
        var mapper = CreateMapper(new NativeManifestDetails { Path = new Uri("app.exe", UriKind.Relative), Arguments = ["--flag"] });
        var catalog = CreateCatalog(Path.Combine(_root, "app.exe"), mapper);

        var action = () => catalog.GetManifest("nativeApp");

        await action.Should().ThrowAsync<NotSupportedException>().WithMessage("*nativeApp*arguments*");
    }

    [Fact]
    public async Task GetManifest_throws_when_the_app_directory_gives_environment_variables_and_they_are_not_allowed()
    {
        var mapper = CreateMapper(new NativeManifestDetails
        {
            Path = new Uri("app.exe", UriKind.Relative),
            EnvironmentVariables = new Dictionary<string, string> { { "MY_VARIABLE", "value" } }
        });
        var catalog = CreateCatalog(Path.Combine(_root, "app.exe"), mapper, new AppDirectoryOptions { AllowNativeArguments = true });

        var action = () => catalog.GetManifest("nativeApp");

        await action.Should().ThrowAsync<NotSupportedException>().WithMessage("*nativeApp*environment variables*");
    }

    [Fact]
    public async Task GetManifest_returns_the_environment_variables_of_the_app_directory_when_they_are_allowed()
    {
        var mapper = CreateMapper(new NativeManifestDetails
        {
            Path = new Uri("app.exe", UriKind.Relative),
            EnvironmentVariables = new Dictionary<string, string> { { "MY_VARIABLE", "value" } }
        });
        var catalog = CreateCatalog(Path.Combine(_root, "app.exe"), mapper, new AppDirectoryOptions { AllowNativeEnvironmentVariables = true });

        var manifest = await catalog.GetManifest("nativeApp");

        GetDetails(manifest).EnvironmentVariables.Should().Equal(new Dictionary<string, string> { { "MY_VARIABLE", "value" } });
    }

    private static NativeManifestDetails GetDetails(IModuleManifest manifest) =>
        ((IModuleManifest<NativeManifestDetails>) manifest).Details;

    private static IHostManifestMapper CreateMapper(NativeManifestDetails details)
    {
        var mapper = new Mock<IHostManifestMapper>();
        mapper.Setup(_ => _.MapModuleDetails(It.IsAny<Fdc3App>())).Returns(details);
        return mapper.Object;
    }

    [Fact]
    public async Task GetAllManifests_skips_the_native_apps_that_cannot_be_started_and_returns_the_other_apps()
    {
        var catalog = CreateCatalogFromApps(
            $$"""
            [
              { "appId": "webApp", "name": "Web App", "type": "web", "details": { "url": "https://example.com/web" } },
              { "appId": "goodApp", "name": "Good", "type": "native", "details": { "path": {{JsonConvert.ToString(Path.Combine(_root, "good.exe"))}} } },
              { "appId": "outsideApp", "name": "Outside", "type": "native", "details": { "path": {{JsonConvert.ToString(Path.Combine(_root + "-evil", "app.exe"))}} } }
            ]
            """);

        var manifests = await catalog.GetAllManifests();

        manifests.Select(m => m.Id).Should().Equal("webApp", "goodApp");
    }

    [Fact]
    public async Task GetAllManifests_skips_the_native_app_whose_path_is_not_a_valid_uri()
    {
        var catalog = CreateCatalogFromApps(
            $$"""
            [
              { "appId": "webApp", "name": "Web App", "type": "web", "details": { "url": "https://example.com/web" } },
              { "appId": "deviceApp", "name": "Device", "type": "native", "details": { "path": {{JsonConvert.ToString(@"\\?\C:\apps\app.exe")}} } },
              { "appId": "goodApp", "name": "Good", "type": "native", "details": { "path": {{JsonConvert.ToString(Path.Combine(_root, "good.exe"))}} } }
            ]
            """);

        var manifests = await catalog.GetAllManifests();

        manifests.Select(m => m.Id).Should().Equal("webApp", "goodApp");
    }

    [Fact]
    public async Task GetManifest_uses_the_allowed_roots_of_the_launch_options()
    {
        var otherRoot = Path.Combine(Path.GetTempPath(), "composeui-other-" + Guid.NewGuid().ToString("N"));
        var launchOptions = new NativeModuleLaunchOptions();
        launchOptions.AllowedRoots.Add(otherRoot);

        var inOtherRoot = await CreateCatalog(Path.Combine(otherRoot, "app.exe"), launchOptions: launchOptions).GetManifest("nativeApp");
        var inEnvironmentRoot = () => CreateCatalog(Path.Combine(_root, "app.exe"), launchOptions: launchOptions).GetManifest("nativeApp");

        GetDetails(inOtherRoot).Path.LocalPath.Should().Be(Path.Combine(otherRoot, "app.exe"));
        await inEnvironmentRoot.Should().ThrowAsync<NotSupportedException>();
    }

    [Fact]
    public async Task GetManifest_returns_a_path_that_is_the_validated_path_even_when_it_contains_percent_sequences()
    {
        var catalog = CreateCatalog("%2e%2e/%2e%2e/outside/evil.exe");

        var manifest = await catalog.GetManifest("nativeApp");

        GetDetails(manifest).Path.LocalPath.Should().Be(Path.Combine(_root, "%2e%2e", "%2e%2e", "outside", "evil.exe"));
    }

    private static IModuleCatalog CreateCatalog(
        string appPath,
        IHostManifestMapper? mapper = null,
        AppDirectoryOptions? options = null,
        NativeModuleLaunchOptions? launchOptions = null) =>
        CreateCatalogFromApps(
            $$"""
            [
              {
                "appId": "nativeApp",
                "name": "Native App",
                "type": "native",
                "details": { "path": {{JsonConvert.ToString(appPath)}} }
              }
            ]
            """,
            mapper,
            options,
            launchOptions);

    private static IModuleCatalog CreateCatalogFromApps(
        string appsJson,
        IHostManifestMapper? mapper = null,
        AppDirectoryOptions? options = null,
        NativeModuleLaunchOptions? launchOptions = null)
    {
        var fileSystem = TestUtils.SetUpFileSystemWithSingleFile(path: "/apps.json", contents: appsJson);

        var appDirectory = new AppDirectory(
            new AppDirectoryOptions { Source = new Uri("file:///apps.json") },
            fileSystem: fileSystem);

        return new Fdc3ModuleCatalog(appDirectory, mapper, options, launchOptions);
    }
}
