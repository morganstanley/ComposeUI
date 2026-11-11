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
using Finos.Fdc3.AppDirectory;
using Microsoft.Extensions.Logging;
using MorganStanley.ComposeUI.Fdc3.AppDirectory.TestUtilities;
using MorganStanley.ComposeUI.ModuleLoader;

namespace MorganStanley.ComposeUI.Fdc3.AppDirectory;

public class Fdc3ModuleCatalogWebUrlTests
{
    public static TheoryData<string> UnsupportedUrls => new()
    {
        "file:///C:/apps/index.html",
        "data:text/html,<script>alert(1)</script>",
        "javascript:alert(1)",
        "blob:https://example.com/3f1b6c1e-0d5a-4a4a-9a53-1c1c3f3a1f55",
        "ftp://example.com/app",
        "ws://example.com/app",
        "about:blank"
    };

    public static TheoryData<string> SupportedUrls => new()
    {
        "https://example.com/app",
        "http://localhost:5000/app",
        "HTTPS://EXAMPLE.COM/app"
    };

    [Theory]
    [MemberData(nameof(UnsupportedUrls))]
    public async Task GetManifest_rejects_a_web_app_with_an_unsupported_url_scheme(string url)
    {
        var catalog = CreateCatalog(url);

        var action = () => catalog.GetManifest("app1");

        await action.Should().ThrowAsync<NotSupportedException>().WithMessage("*app1*");
    }

    [Theory]
    [MemberData(nameof(SupportedUrls))]
    public async Task GetManifest_accepts_a_web_app_with_an_http_or_https_url(string url)
    {
        var catalog = CreateCatalog(url);

        var manifest = await catalog.GetManifest("app1");

        manifest.TryGetDetails<WebManifestDetails>(out var details).Should().BeTrue();
        details.Url.Should().Be(new Uri(url, UriKind.Absolute));
    }

    private static IModuleCatalog CreateCatalog(string url)
    {
        var apps = new[]
        {
            new { appId = "app1", name = "App", type = "web", details = new { url } }
        };

        var fileSystem = TestUtils.SetUpFileSystemWithSingleFile("/apps.json", JsonSerializer.Serialize(apps));

        var appDirectory = new AppDirectory(
            new AppDirectoryOptions { Source = new Uri("file:///apps.json") },
            fileSystem: fileSystem);

        return new Fdc3ModuleCatalog(appDirectory);
    }

    [Fact]
    public async Task GetAllManifests_leaves_out_web_apps_with_an_unsupported_url_scheme_and_logs_a_warning()
    {
        var logger = new Mock<ILogger<Fdc3ModuleCatalog>>();
        var catalog = CreateCatalog(
            logger.Object,
            ("app1", "https://example.com/app1"),
            ("app2", "file:///C:/apps/index.html"),
            ("app3", "https://example.com/app3"),
            ("app4", "javascript:alert(1)"));

        var manifests = await catalog.GetAllManifests();

        manifests.Select(manifest => manifest.Id).Should().BeEquivalentTo("app1", "app3");
        VerifyWarningLogged(logger, "app2");
        VerifyWarningLogged(logger, "app4");
    }

    [Fact]
    public async Task GetAllManifests_returns_every_app_when_all_urls_are_supported()
    {
        var logger = new Mock<ILogger<Fdc3ModuleCatalog>>();
        var catalog = CreateCatalog(logger.Object, ("app1", "https://example.com/app1"), ("app2", "http://localhost:5000/app2"));

        var manifests = await catalog.GetAllManifests();

        manifests.Select(manifest => manifest.Id).Should().BeEquivalentTo("app1", "app2");
        logger.Invocations.Where(invocation => invocation.Method.Name == nameof(ILogger.Log)).Should().BeEmpty();
    }

    private static void VerifyWarningLogged(Mock<ILogger<Fdc3ModuleCatalog>> logger, string appId) =>
        logger.Verify(
            l => l.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((state, _) => state.ToString()!.Contains(appId)),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

    private static IModuleCatalog CreateCatalog(ILogger<Fdc3ModuleCatalog> logger, params (string AppId, string Url)[] apps)
    {
        var contents = JsonSerializer.Serialize(
            apps.Select(app => new { appId = app.AppId, name = app.AppId, type = "web", details = new { app.Url } }));
        var fileSystem = TestUtils.SetUpFileSystemWithSingleFile("/apps.json", contents.Replace("\"Url\"", "\"url\""));

        var appDirectory = new AppDirectory(
            new AppDirectoryOptions { Source = new Uri("file:///apps.json") },
            fileSystem: fileSystem);

        return new Fdc3ModuleCatalog(appDirectory, logger: logger);
    }
}
