﻿/*
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

using Finos.Fdc3.AppDirectory;

namespace MorganStanley.ComposeUI.Fdc3.AppDirectory;

public partial class AppDirectoryTests
{
    [Theory]
    [InlineData("app1")]
    [InlineData("APP1")]
    public async Task GetApp_returns_the_app_with_the_specified_appId_ignoring_case(string appId)
    {
        var fileSystem = TestUtils.SetUpFileSystemWithSingleFile(
            path: "/apps.json",
            contents: """
            [
              {
                "appId": "app1",
                "name": "App",
                "type": "web",
                "details": { "url": "https://example.com/app1" }
              }
            ]
            """);

        var appDirectory = new AppDirectory(
            new AppDirectoryOptions {Source = new Uri("file:///apps.json")},
            fileSystem: fileSystem);

        var app = await appDirectory.GetApp(appId);
        app.Should().NotBeNull();
        app.Should()
            .Match<Fdc3App<WebAppDetails>>(
                x => x.AppId == "app1"
                     && x.Name == "App"
                     && x.Type == AppType.Web
                     && x.Details.Url == "https://example.com/app1");
    }

    [Fact]
    public async Task GetApp_throws_AppNotFoundException_if_the_app_is_not_found()
    {
        var fileSystem = TestUtils.SetUpFileSystemWithSingleFile(
            path: "/apps.json",
            contents: """
            [
              {
                "appId": "app1",
                "name": "App",
                "type": "web",
                "details": { "url": "https://example.com/app1" }
              }
            ]
            """);

        var appDirectory = new AppDirectory(
            new AppDirectoryOptions {Source = new Uri("file:///apps.json")},
            fileSystem: fileSystem);

        await this.Awaiting(_ => appDirectory.GetApp("x")).Should().ThrowAsync<AppNotFoundException>();
    }
}