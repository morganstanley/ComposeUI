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

using System;
using Microsoft.Extensions.DependencyInjection;
using MorganStanley.ComposeUI.ProcessExplorer.Abstractions.Entities;
using MorganStanley.ComposeUI.ProcessExplorer.Client;
using Xunit;

namespace MorganStanley.ComposeUI.ProcessExplorer.LocalHandler.Tests;

public class InformationHandlerHelperTests
{
    [Fact]
    public void GetModulesFromAssembly_will_return_some_value()
    {
        var result = InformationHandlerHelper.GetModulesFromAssembly();
        Assert.NotNull(result);
        Assert.NotEmpty(result);
    }

    [Fact]
    public void GetEnvironmentVariablesFromAssembly_will_return_some_value()
    {
        var result = InformationHandlerHelper.GetEnvironmentVariablesFromAssembly();
        Assert.NotNull(result);
        Assert.NotEmpty(result);
    }

    [Fact]
    public void GetRegistrations_will_return_some_value()
    {
        var dummyServiceCollection = new ServiceCollection();
        dummyServiceCollection.AddSingleton<IFakeService, DummyFakeService>();
        var result = InformationHandlerHelper.GetRegistrations(dummyServiceCollection);

        Assert.NotNull(result);
        Assert.Single(result);

        var expectedRegistration = new RegistrationInfo()
        {
            ImplementationType = nameof(DummyFakeService),
            ServiceType = nameof(IFakeService),
            LifeTime = "Singleton"
        };

        Assert.Contains(expectedRegistration, result);
    }

    private interface IFakeService
    {
        void Dummy();
    }

    private class DummyFakeService : IFakeService
    {
        public void Dummy()
        {
            throw new NotImplementedException();
        }
    }
}
