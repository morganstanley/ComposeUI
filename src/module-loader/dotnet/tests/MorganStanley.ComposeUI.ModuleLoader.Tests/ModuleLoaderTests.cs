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

namespace MorganStanley.ComposeUI.ModuleLoader.Tests
{
    public class ModuleLoaderTests
    {
        [Fact]
        public async Task Test1()
        {
            var serviceProvider = new ServiceCollection()
                .AddModuleLoader()
                .AddSingleton<IModuleCatalog, MockModuleCatalog>()
                .AddSingleton<IStartupAction, MockStartupAction>()
                .BuildServiceProvider();

            var moduleLoader = serviceProvider.GetRequiredService<IModuleLoader>();
            moduleLoader.LifetimeEvents.Subscribe(x =>
            {

            });

            await moduleLoader.StartModule(new StartRequest("Google"));
        }
    }

    public class MockModuleCatalog : IModuleCatalog
    {
        public IModuleManifest GetManifest(string moduleId)
        {
            return new MockModuleManifest();
        }

        public IEnumerable<string> GetModuleIds()
        {
            return new[] { "Google" };
        }
    }

    public class MockModuleManifest : IModuleManifest<WebManifestDetails>
    {
        public WebManifestDetails Details => new WebManifestDetails
        {
            IconUrl = new Uri("https://www.google.com/favicon.ico"),
            Url = new Uri("https://www.google.com")
        };

        public string Id => "Google";

        public string Name => "Google";

        public string ModuleType => ComposeUI.ModuleLoader.ModuleType.Web;
    }

    public class MockStartupAction : IStartupAction
    {
        public async Task InvokeAsync(StartupContext startupContext, Func<Task> next)
        {
            startupContext.AddProperty("preload script");

            await next();
        }
    }
}