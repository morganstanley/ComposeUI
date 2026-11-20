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
using System.Text.RegularExpressions;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using MorganStanley.ComposeUI.Fdc3.AppDirectory;
using MorganStanley.ComposeUI.ModuleLoader;

namespace MorganStanley.ComposeUI.Shell.Fdc3;

public class ExampleAppDirectoryTests
{
    [Fact]
    public async Task The_native_example_app_can_be_started_with_the_options_of_the_Shell_launch_profile()
    {
        var repositoryRoot = FindRepositoryRoot();
        var projectDirectory = Path.Combine(repositoryRoot, "src", "shell", "dotnet", "src", "Shell");

        using var launchSettings = JsonDocument.Parse(File.ReadAllText(Path.Combine(projectDirectory, "Properties", "launchSettings.json")));
        var profile = launchSettings.RootElement.GetProperty("profiles").GetProperty("Shell");

        var arguments = ExpandProperties(profile.GetProperty("commandLineArgs").GetString()!, repositoryRoot, projectDirectory);
        var moduleRoot = ExpandProperties(
            profile.GetProperty("environmentVariables").GetProperty(NativeModuleLaunchOptions.RepositoryRootEnvironmentVariableName).GetString()!,
            repositoryRoot,
            projectDirectory);

        var configuration = new ConfigurationBuilder().AddCommandLine(SplitArguments(arguments)).Build();
        var options = new AppDirectoryOptions();
        configuration.GetSection("FDC3:AppDirectory").Bind(options);

        var launchOptions = new NativeModuleLaunchOptions();
        launchOptions.AllowedRoots.Add(Path.GetFullPath(moduleRoot));

        var mapper = new ComposeUIHostManifestMapper();
        var catalog = new Fdc3ModuleCatalog(new AppDirectory(options, hostManifestMapper: mapper), mapper, options, launchOptions);

        var manifest = await catalog.GetManifest("WPFExample");

        manifest.Id.Should().Be("WPFExample");
        (await catalog.GetAllManifests()).Select(m => m.Id).Should().Contain("WPFExample");
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "examples", "fdc3-appdirectory", "apps-with-intents.json")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new InvalidOperationException("The repository root was not found.");
    }

    // Stands in for the replacement of the MSBuild properties that Visual Studio does when it starts a launch profile.
    private static string ExpandProperties(string value, string repositoryRoot, string projectDirectory) => value
        .Replace("$(ComposeUIRepositoryRoot)", repositoryRoot)
        .Replace("$(ProjectDir)", projectDirectory + Path.DirectorySeparatorChar);

    private static string[] SplitArguments(string arguments) => Regex.Matches(arguments, "\"([^\"]*)\"|(\\S+)")
        .Select(m => m.Groups[1].Success ? m.Groups[1].Value : m.Groups[2].Value)
        .ToArray();
}
