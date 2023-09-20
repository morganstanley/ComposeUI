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

using MorganStanley.Fdc3.AppDirectory;
using TaskExtensions = MorganStanley.ComposeUI.Testing.TaskExtensions;

namespace MorganStanley.ComposeUI.Fdc3.AppDirectory;

public partial class AppDirectoryTests
{
    [Fact]
    public async Task GetApps_loads_the_data_from_a_file()
    {
        var fileSystem = TestUtils.SetUpFileSystemWithSingleFile(
            path: "/apps.json",
            contents: """
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
            """);

        var appDirectory = new AppDirectory(
            new AppDirectoryOptions {Source = new Uri("file:///apps.json")},
            fileSystem: fileSystem);

        var apps = await appDirectory.GetApps();

        apps.Should().NotBeNull();
        apps.Should().HaveCount(2);
        apps.Should()
            .BeEquivalentTo(
                new[]
                {
                    new Fdc3App(
                        "webApp",
                        "Web App",
                        AppType.Web,
                        new WebAppDetails("https://example.com/webApp")),

                    new Fdc3App(
                        "nativeApp",
                        "Native App",
                        AppType.Native,
                        new NativeAppDetails("path/to/app", arguments: null))
                });
    }

    [Fact]
    public async Task GetApps_reloads_the_data_if_the_source_file_has_changed()
    {
        var fileSystem = TestUtils.SetUpFileSystemWithSingleFile(
            path: "/apps.json",
            contents: """
            [
              {
                "appId": "app1",
                "name": "App 1",
                "type": "web",
                "details": { "url": "https://example.com/app1" }
              }
            ]
            """);

        var appDirectory = new AppDirectory(
            new AppDirectoryOptions {Source = new Uri("file:///apps.json")},
            fileSystem: fileSystem);

        var apps = await appDirectory.GetApps();

        apps.Select(x => x.AppId).Should().BeEquivalentTo("app1");

        await fileSystem.File.WriteAllTextAsync(
            "/apps.json",
            """
            [
              {
                "appId": "app1",
                "name": "App 1",
                "type": "web",
                "details": { "url": "https://example.com/app1" }
              },
              {
                "appId": "app2",
                "name": "App 2",
                "type": "web",
                "details": { "url": "https://example.com/app2" }
              }
            ]
            """);

        await TaskExtensions.WaitForBackgroundTasksAsync();
        apps = await appDirectory.GetApps();

        apps.Select(x => x.AppId).Should().BeEquivalentTo("app1", "app2");
    }
}