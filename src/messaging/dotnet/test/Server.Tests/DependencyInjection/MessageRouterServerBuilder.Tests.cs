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

using Microsoft.Extensions.DependencyInjection;
using MorganStanley.ComposeUI.Messaging.Server;

namespace MorganStanley.ComposeUI.Messaging.DependencyInjection;

public class MessageRouterServerBuilderTests
{
    [Fact]
    public void UseClientAuthenticator_registers_the_authenticator_instance()
    {
        var authenticator = new Mock<IClientAuthenticator>().Object;
        var services = new ServiceCollection();
        var builder = new MessageRouterServerBuilder(services);

        var result = builder.UseClientAuthenticator(authenticator);

        result.Should().BeSameAs(builder);
        using var serviceProvider = services.BuildServiceProvider();
        serviceProvider.GetRequiredService<IClientAuthenticator>().Should().BeSameAs(authenticator);
    }

    [Fact]
    public void UseClientAuthenticator_registers_the_authenticator_factory()
    {
        var authenticator = new Mock<IClientAuthenticator>().Object;
        var services = new ServiceCollection();
        var builder = new MessageRouterServerBuilder(services);

        var result = builder.UseClientAuthenticator(_ => authenticator);

        result.Should().BeSameAs(builder);
        using var serviceProvider = services.BuildServiceProvider();
        serviceProvider.GetRequiredService<IClientAuthenticator>().Should().BeSameAs(authenticator);
    }
}
