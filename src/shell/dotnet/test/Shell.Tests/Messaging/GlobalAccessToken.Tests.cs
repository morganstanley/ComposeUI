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

using System.Reflection;
using FluentAssertions;

namespace MorganStanley.ComposeUI.Shell.Messaging;

/// <summary>
/// Regression tests: the shell used to share a single message router access token between all modules.
/// </summary>
public class GlobalAccessTokenTests
{
    private const BindingFlags AllMembers =
        BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic |
        BindingFlags.DeclaredOnly;

    [Fact]
    public void The_global_message_router_access_token_no_longer_exists()
    {
        var shellTypes = typeof(App).Assembly.GetTypes();

        var members = shellTypes
            .SelectMany(type => type.GetMembers(AllMembers))
            .Where(member => member.Name == "MessageRouterAccessToken")
            .Select(member => $"{member.DeclaringType!.FullName}.{member.Name}");

        members.Should().BeEmpty();
    }

    [Theory]
    [InlineData(typeof(WebModuleMessageRouterConfigurator))]
    [InlineData(typeof(NativeModuleMessageRouterConfigurator))]
    public void The_configurators_require_a_token_store(Type configuratorType)
    {
        var constructors = configuratorType.GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        constructors.Should().OnlyContain(
            ctor => ctor.GetParameters().Any(p => p.ParameterType == typeof(IModuleTokenStore)));
    }
}
