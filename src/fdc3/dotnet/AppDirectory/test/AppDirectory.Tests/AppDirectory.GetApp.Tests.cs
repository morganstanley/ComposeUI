using MorganStanley.Fdc3.AppDirectory;

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