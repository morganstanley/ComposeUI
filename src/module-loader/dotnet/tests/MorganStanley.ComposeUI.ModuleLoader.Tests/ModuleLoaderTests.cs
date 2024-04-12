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
using Moq;

namespace MorganStanley.ComposeUI.ModuleLoader.Tests
{
    public class ModuleLoaderTests
    {
        [Fact]
        public void GivenNullArguments_WhenCtor_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new ModuleLoader(null!, Enumerable.Empty<IModuleRunner>(), Enumerable.Empty<IStartupAction>()));
            Assert.Throws<ArgumentNullException>(() => new ModuleLoader(new Mock<IEnumerable<IModuleCatalog>>().Object, null!, Enumerable.Empty<IStartupAction>()));
            Assert.Throws<ArgumentNullException>(() => new ModuleLoader(new Mock<IEnumerable<IModuleCatalog>>().Object, Enumerable.Empty<IModuleRunner>(), null!));
        }

        [Fact]
        public void GivenUnknownModuleId_WhenStart_ThrowsException()
        {
            var moduleCatalogMock = new Mock<IModuleCatalog>();
            moduleCatalogMock.Setup(c => c.GetManifest(It.IsAny<string>())).Returns(Task.FromResult<IModuleManifest?>(null));

            var moduleLoader = new ModuleLoader(new[] { moduleCatalogMock.Object }, Enumerable.Empty<IModuleRunner>(), Enumerable.Empty<IStartupAction>());
            Assert.ThrowsAsync<Exception>(async () => await moduleLoader.StartModule(new StartRequest("invalid")));
        }

        [Fact]
        public void WhenNoModuleRunnerAvailable_WhenStart_ThrowsException()
        {
            var moduleManifestMock = new Mock<IModuleManifest>();
            moduleManifestMock.Setup(m => m.ModuleType).Returns("test");
            var moduleCatalogMock = new Mock<IModuleCatalog>();
            moduleCatalogMock.Setup(c => c.GetManifest(It.IsAny<string>())).Returns(Task.FromResult<IModuleManifest>(moduleManifestMock.Object));
            var testModuleRunnerMock = new Mock<IModuleRunner>();
            testModuleRunnerMock.Setup(r => r.ModuleType).Returns("other");

            var moduleLoader = new ModuleLoader(new[] { moduleCatalogMock.Object }, new[] { testModuleRunnerMock.Object }, Enumerable.Empty<IStartupAction>());
            Assert.ThrowsAsync<Exception>(async () => await moduleLoader.StartModule(new StartRequest("valid")));
        }

        [Fact]
        public async Task StartModule_EndToEndTest()
        {
            const string moduleId = "Google";
            const string testProperty = "test property";

            var moduleCatalogMock = new Mock<IModuleCatalog>();
            var startupActionMock = new Mock<IStartupAction>();
            var moduleManifestMock = new Mock<IModuleManifest<WebManifestDetails>>();
            var webManifestDetails = new WebManifestDetails
            {
                IconUrl = new Uri("https://www.google.com/favicon.ico"),
                Url = new Uri("https://www.google.com")
            };

            moduleManifestMock.Setup(m => m.ModuleType).Returns(ModuleType.Web);
            moduleManifestMock.Setup(m => m.Details).Returns(webManifestDetails);
            moduleCatalogMock.Setup(c => c.GetManifest(moduleId)).Returns(Task.FromResult<IModuleManifest>(moduleManifestMock.Object));
            moduleCatalogMock.Setup(catalog => catalog.GetModuleIds()).Returns(Task.FromResult<IEnumerable<string>>(new[] { moduleId }));
            startupActionMock.Setup(s => s.InvokeAsync(It.IsAny<StartupContext>(), It.IsAny<Func<Task>>()))
                .Callback<StartupContext, Func<Task>>((startupContext, next) =>
                {
                    startupContext.AddProperty(testProperty);
                })
                .Returns(Task.CompletedTask);

            var serviceProvider = new ServiceCollection()
                .AddModuleLoader()
                .AddSingleton<IModuleCatalog>(moduleCatalogMock.Object)
                .AddSingleton<IStartupAction>(startupActionMock.Object)
                .BuildServiceProvider();

            var moduleLoader = serviceProvider.GetRequiredService<IModuleLoader>();
            bool startingEventReceived = false;
            bool startedEventReceived = false;
            moduleLoader.LifetimeEvents.Subscribe(x =>
            {
                if (x.EventType == LifetimeEventType.Starting)
                    startingEventReceived = true;

                if (x.EventType == LifetimeEventType.Started)
                    startedEventReceived = true;
            });

            var startRequest = new StartRequest(moduleId);
            var moduleInstance = await moduleLoader.StartModule(startRequest);
            Assert.NotNull(moduleInstance);
            Assert.Equal(moduleManifestMock.Object, moduleInstance.Manifest);
            Assert.Equal(startRequest, moduleInstance.StartRequest);
            var allProperties = moduleInstance.GetProperties();

            var webProperties = allProperties.OfType<WebStartupProperties>().Single();
            Assert.Equal(webManifestDetails.IconUrl, webProperties.IconUrl);
            Assert.Equal(webManifestDetails.Url, webProperties.Url);

            var stringProperty = allProperties.OfType<string>().Single();
            Assert.Equal(testProperty, stringProperty);

            Assert.True(startingEventReceived);
            Assert.True(startedEventReceived);
        }
    }
}