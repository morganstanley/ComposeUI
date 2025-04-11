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
using Moq;
using MorganStanley.ComposeUI.ModuleLoader.Runners;

namespace MorganStanley.ComposeUI.ModuleLoader.Tests.Runners;

public class WebModuleRunnerTests
{
    [Fact]
    public void ModuleType_Is_Web()
    {
        var runner = new WebModuleRunner();
        runner.ModuleType.Should().Be(ModuleType.Web);
    }

    [Fact]
    public async Task WhenStart_WebStartupPropertiesAreAddedToStartupContext()
    {
        var moduleInstanceMock = new Mock<IModuleInstance>();
        WebManifestDetails details = new() { IconUrl = new Uri("http://test.uri"), Url = new Uri("http://test2.uri") };
        var moduleManifestMock = new MockModuleManifest(details);
        moduleInstanceMock.Setup(m => m.Manifest).Returns(moduleManifestMock);
        var startRequest = new StartRequest("test");
        var startupContext = new StartupContext(startRequest, moduleInstanceMock.Object);
        static Task MockPipeline() => Task.CompletedTask;

        var runner = new WebModuleRunner();
        await runner.Start(startupContext, MockPipeline);

        var result = startupContext.GetProperties();
        result.Should().NotBeNull();
        var webProperties = result.OfType<WebStartupProperties>().Single();
        details.IconUrl.Should().BeEquivalentTo(webProperties.IconUrl);
        details.Url.Should().BeEquivalentTo(webProperties.Url);
    }

    private class MockModuleManifest : IModuleManifest<WebManifestDetails>
    {
        public MockModuleManifest(WebManifestDetails details)
        {
            Details = details;
        }

        public string Id => throw new NotImplementedException();

        public string Name => throw new NotImplementedException();

        public string ModuleType => throw new NotImplementedException();

        public WebManifestDetails Details { get; }

        public string[] Tags => throw new NotImplementedException();

        public Dictionary<string, string> AdditionalProperties => throw new NotImplementedException();
    }
}
