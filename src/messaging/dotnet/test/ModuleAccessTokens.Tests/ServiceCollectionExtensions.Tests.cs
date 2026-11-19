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

using System.Reactive.Subjects;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Moq;
using MorganStanley.ComposeUI.ModuleLoader;

namespace MorganStanley.ComposeUI.Messaging.Server.ModuleAccessTokens;

public class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddModuleAccessTokens_returns_the_registered_token_store()
    {
        var services = new ServiceCollection();

        var tokenStore = services.AddModuleAccessTokens();

        using var serviceProvider = services.BuildServiceProvider();
        serviceProvider.GetRequiredService<IModuleTokenStore>().Should().BeSameAs(tokenStore);
    }

    [Fact]
    public async Task AddModuleAccessTokens_registers_an_authenticator_that_accepts_the_tokens_of_the_store()
    {
        var services = new ServiceCollection();
        var tokenStore = services.AddModuleAccessTokens();
        await using var serviceProvider = services.BuildServiceProvider();

        var authenticator = serviceProvider.GetRequiredService<IClientAuthenticator>();
        var principal = await authenticator.Authenticate("client-1", tokenStore.Issue(Guid.NewGuid(), "module-a"));

        principal.Identity!.Name.Should().Be("module-a");
    }

    [Fact]
    public async Task AddModuleAccessTokens_registers_the_revocation_service()
    {
        var services = new ServiceCollection();
        var tokenStore = services.AddModuleAccessTokens();
        var lifetimeEvents = new Subject<LifetimeEvent>();
        var moduleLoader = new Mock<IModuleLoader>();
        moduleLoader.SetupGet(_ => _.LifetimeEvents).Returns(lifetimeEvents);
        services.AddSingleton(moduleLoader.Object);
        await using var serviceProvider = services.BuildServiceProvider();

        // Resolving the hosted services is what the host does before it starts them.
        var hostedServices = serviceProvider.GetServices<IHostedService>().ToList();

        hostedServices.Should().ContainSingle();
        var instanceId = Guid.NewGuid();
        var token = tokenStore.Issue(instanceId, "module-a");
        lifetimeEvents.OnNext(new LifetimeEvent.Stopped(CreateInstance(instanceId)));
        tokenStore.TryValidate(token, out _).Should().BeFalse();
    }

    private static IModuleInstance CreateInstance(Guid instanceId)
    {
        var instance = new Mock<IModuleInstance>();
        instance.SetupGet(_ => _.InstanceId).Returns(instanceId);
        return instance.Object;
    }
}
