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

using FluentAssertions;
using Moq;
using MorganStanley.ComposeUI.ModuleLoader;

namespace MorganStanley.ComposeUI.Shell.Preloading;

public class DefaultPreloadScriptPolicyTests
{
    private readonly Mock<IModuleInstance> _moduleInstanceMock;

    public DefaultPreloadScriptPolicyTests()
    {
        var properties = new object[]
        {
            new WebStartupProperties()
            {
                AllowedOrigins = new List<Uri>()
                {
                    new("https://www.morganstanley.com/"),
                    new("https://www.microsoft.com/"),
                    new("https://www.google.com/")
                }
            }
        };

        _moduleInstanceMock = new Mock<IModuleInstance>();
        
        _moduleInstanceMock
            .Setup(_ => _.GetProperties())
            .Returns(properties);
    }

    [Theory]
    [InlineData("https://www.github.com/", "https://www.github.com/morganstanley/ComposeUI")]
    [InlineData("https://www.github.com", "https://www.github.com/")]
    public async Task IsPreloadingScriptsAllowedAsync_returns_true_based_on_base_url(string baseAppUrl, string urlToNavigate)
    {
        var preloadPolicy = new DefaultPreloadScriptPolicy(_moduleInstanceMock.Object);
        var result = await preloadPolicy.IsPreloadingScriptsAllowedAsync(
            new Uri(baseAppUrl),
            new Uri(urlToNavigate));

        result.Should().BeTrue();
    }

    [Theory]
    [InlineData("https://www.github.com", "https://www.microsoft.com/")]
    [InlineData("https://www.github.com", "https://www.morganstanley.com/")]
    [InlineData("https://www.github.com", "https://www.google.com/")]
    [InlineData("https://www.github.com", "https://www.google.com")]
    public async Task IsPreloadingScriptsAllowedAsync_returns_true_based_on_the_allowed_origins(string baseAppUrl, string urlToNavigate)
    {
        var preloadPolicy = new DefaultPreloadScriptPolicy(_moduleInstanceMock.Object);
        var result = await preloadPolicy.IsPreloadingScriptsAllowedAsync(
            new Uri(baseAppUrl),
            new Uri(urlToNavigate));

        result.Should().BeTrue();
    }

    [Theory]
    [InlineData("https://www.github.com/", "http://www.github.com")]
    [InlineData("https://www.github.com/", "http://www.morganstanley.com")]
    public async Task IsPreloadingScriptsAllowedAsync_returns_false(string baseAppUrl, string urlToNavigate)
    {
        var preloadPolicy = new DefaultPreloadScriptPolicy(_moduleInstanceMock.Object);
        var result = await preloadPolicy.IsPreloadingScriptsAllowedAsync(
            new Uri(baseAppUrl),
            new Uri(urlToNavigate));

        result.Should().BeFalse();
    }
}
