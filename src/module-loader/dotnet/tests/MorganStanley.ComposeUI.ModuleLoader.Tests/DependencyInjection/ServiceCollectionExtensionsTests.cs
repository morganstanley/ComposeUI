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
using Microsoft.Extensions.DependencyInjection;
using MorganStanley.ComposeUI.ModuleLoader.Runners;

namespace MorganStanley.ComposeUI.ModuleLoader.Tests;

public class ServiceCollectionExtensionsTests
{
    [Fact]
    public void WhenAddModuleLoader_TypesAreAddedToServiceCollection()
    {
        var services = new ServiceCollection()
            .AddModuleLoader();

        services.Count(s => s.ServiceType == typeof(IModuleLoader) && s.ImplementationType == typeof(ModuleLoader)).Should().Be(1);
        services.Count(s => s.ServiceType == typeof(IModuleRunner) && s.ImplementationType == typeof(WebModuleRunner)).Should().Be(1);
        services.Count(s => s.ServiceType == typeof(IModuleRunner) && s.ImplementationType == typeof(NativeModuleRunner)).Should().Be(1);
    }
}
