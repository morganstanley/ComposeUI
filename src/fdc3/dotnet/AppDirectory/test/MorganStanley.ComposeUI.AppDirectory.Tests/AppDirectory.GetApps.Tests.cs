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

using System.Net;
using System.Text;
using Moq.Contrib.HttpClient;
using Finos.Fdc3.AppDirectory;
using Newtonsoft.Json.Linq;
using TaskExtensions = MorganStanley.ComposeUI.Testing.TaskExtensions;
using System.IO.Abstractions;

namespace MorganStanley.ComposeUI.Fdc3.AppDirectory;

public partial class AppDirectoryTests
{
    [Theory, CombinatorialData]
    public async Task GetApps_loads_the_data_from_a_file(
        bool useApiSchema)
    {
        var source = "/apps.json";
        var json = useApiSchema ? GetAppsApiResponse : GetAppsJsonArray;

        var fileSystem = TestUtils.SetUpFileSystemWithSingleFile(
            source,
            json);

        var appDirectory = new AppDirectory(
            new AppDirectoryOptions { Source = new Uri($"file://{source}") },
            fileSystem: fileSystem);

        var apps = await appDirectory.GetApps();

        apps.Should().BeEquivalentTo(GetAppsExpectation);
    }

    [Theory, CombinatorialData]
    public async Task GetApps_reloads_the_data_if_the_source_file_has_changed(bool useApiSchema)
    {
        await Task.Yield(); // Finish other tests before running this one, as it uses the whole threadpool

        var source = "/apps.json";
        var json = useApiSchema ? GetAppsApiResponse : GetAppsJsonArray;

        var fileSystem = TestUtils.SetUpFileSystemWithSingleFile(
            source,
            json);

        var appDirectory = new AppDirectory(
            new AppDirectoryOptions { Source = new Uri($"file://{source}") },
            fileSystem: fileSystem);

        _ = await appDirectory.GetApps();

        await fileSystem.File.WriteAllTextAsync(
            source,
            useApiSchema ? GetAppsApiResponseChanged : GetAppsJsonArrayChanged,
            Encoding.UTF8);

        await TaskExtensions.WaitForBackgroundTasksAsync(TimeSpan.FromSeconds(20));

        var apps = await appDirectory.GetApps();

        apps.Should().BeEquivalentTo(GetAppsExpectationChanged);
    }

    [Theory, CombinatorialData]
    public async Task GetApps_loads_the_data_from_http_Source(bool setHttpClientName, bool useApiSchema)
    {
        var json = useApiSchema ? GetAppsApiResponse : GetAppsJsonArray;
        var source = new Uri("https://example.com/apps");
        var httpClientName = setHttpClientName ? "http-client-name" : null;
        var handler = new Mock<HttpMessageHandler>();

        handler.SetupRequest(HttpMethod.Get, source)
            .ReturnsResponse(HttpStatusCode.OK, json);

        var httpClientFactory = handler.CreateClientFactory();

        if (setHttpClientName)
        {
            Mock.Get(httpClientFactory)
                .Setup(_ => _.CreateClient(It.IsAny<string>()))
                .Returns(
                    (string name) =>
                    {
                        Assert.Equal(name, httpClientName);

                        return handler.CreateClient();
                    });
        }

        var appDirectory = new AppDirectory(
            new AppDirectoryOptions { Source = source, HttpClientName = httpClientName },
            httpClientFactory: httpClientFactory);

        var apps = await appDirectory.GetApps();

        apps.Should().BeEquivalentTo(GetAppsExpectation);
    }

    [Theory, CombinatorialData]
    public async Task GetApps_loads_the_data_from_http_api_when_HttpClientName_is_set(bool useApiSchema)
    {
        var json = useApiSchema ? GetAppsApiResponse : GetAppsJsonArray;
        var httpClientName = "http-client-name";
        var handler = new Mock<HttpMessageHandler>();

        handler.SetupRequest(HttpMethod.Get, "https://example.com/api/v2/apps/")
            .ReturnsResponse(HttpStatusCode.OK, json);

        var httpClientFactory = handler.CreateClientFactory();
        Mock.Get(httpClientFactory)
            .Setup(_ => _.CreateClient(httpClientName))
            .Returns(
                () =>
                {
                    var client = handler.CreateClient();
                    client.BaseAddress = new Uri("https://example.com/api/v2/");
                    return client;
                });

        var appDirectory = new AppDirectory(
            new AppDirectoryOptions { HttpClientName = httpClientName },
            httpClientFactory: httpClientFactory);

        var apps = await appDirectory.GetApps();

        apps.Should().BeEquivalentTo(GetAppsExpectation);
    }

    private const string GetAppsJsonArray = """
        [
          {
            "appId": "webApp",
            "name": "Web App",
            "type": "web",
            "details": { "url": "https://example.com/webApp" }
        },
          {
            "appId": "nativeApp",
            "name": "Native App",
            "type": "native",
            "details": { "path": "path/to/app" }
          }
        ]
        """;

    private static readonly List<Fdc3App> GetAppsExpectation =
        new()
        {
            new Fdc3App(
                appId: "webApp",
                name: "Web App",
                AppType.Web,
                new WebAppDetails("https://example.com/webApp")),

            new Fdc3App(
                appId: "nativeApp",
                name: "Native App",
                AppType.Native,
                new NativeAppDetails(path: "path/to/app", arguments: null))
        };

    private const string GetAppsJsonArrayChanged = """
        [
          {
            "appId": "webApp",
            "name": "Web App",
            "type": "web",
            "details": { "url": "https://example.com/webApp" }
          },
          {
            "appId": "nativeApp",
            "name": "Native App",
            "type": "native",
            "details": { "path": "path/to/app" }
          },
          {
            "appId": "otherApp",
            "name": "Other App",
            "type": "other",
            "details": { "foo": "bar" }
          }
        ]
        """;

    private static readonly List<Fdc3App> GetAppsExpectationChanged =
        new()
        {
            new Fdc3App(
                appId: "webApp",
                name: "Web App",
                AppType.Web,
                new WebAppDetails("https://example.com/webApp")),

            new Fdc3App(
                appId: "nativeApp",
                name: "Native App",
                AppType.Native,
                new NativeAppDetails(path: "path/to/app", arguments: null)),

            new Fdc3App(
                appId: "otherApp",
                name: "Other App",
                AppType.Other,
                JObject.Parse("{ 'foo': 'bar' }"))
        };

    private const string GetAppsApiResponse = $$"""
        {
          "applications": {{GetAppsJsonArray}}
        }
        """;

    private const string GetAppsApiResponseChanged = $$"""
        {
          "applications": {{GetAppsJsonArrayChanged}}
        }
        """;
}