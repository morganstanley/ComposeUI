/// ********************************************************************************************************
///
/// Morgan Stanley makes this available to you under the Apache License, Version 2.0 (the "License").
/// You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0.
/// See the NOTICE file distributed with this work for additional information regarding copyright ownership.
/// Unless required by applicable law or agreed to in writing, software distributed under the License
/// is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
/// See the License for the specific language governing permissions and limitations under the License.
/// 
/// ********************************************************************************************************

using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using MorganStanley.ComposeUI.Tryouts.Core.Abstractions;
using NP.Avalonia.Gidon;
using NP.IoCy;
using Subscriptions;
using System.IO;

namespace MorganStanley.ComposeUI.Prototypes.WV2Prototype
{
    public class App : Application
    {
        private ICommunicationService _server;

        /// defined the Gidon plugin manager
        /// use the following paths (relative to the PluginsPrototype.exe executable)
        /// to dynamically load the plugins and services:
        /// "Plugins/Services" - to load the services (non-visual singletons)
        /// "Plugins/ViewModelPlugins" - to load view model plugins
        /// "Plugins/ViewPlugins" - to load view plugins
        public static PluginManager _pluginManager { get; } =
            new PluginManager
            (
                "Plugins/Services",
                "Plugins/ViewModelPlugins",
                "Plugins/ViewPlugins");

        // the IoC container
        private static IoCContainer _container = _pluginManager.TheContainer;

        internal ISubscriptionClient SubscriptionClient;

        public App()
        {
            // inject all dynamically loaded assemblies
            _pluginManager.CompleteConfiguration();

            _server = _container.Resolve<ICommunicationService>();

            SubscriptionClient = _container.Resolve<ISubscriptionClient>();

            _server.AddTopics(("Test", typeof(TestTopicMessage)));

            _ = SubscriptionClient.Connect
                (
                    CommunicationsConstants.MachineName, 
                    CommunicationsConstants.Port);
        }

        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.MainWindow = new MainWindow();

                desktop.MainWindow.Closed += OnMainWindowClosed;

                desktop.Startup += OnDesktopStartup;
            }

            base.OnFrameworkInitializationCompleted();
        }

        private void OnMainWindowClosed(object? sender, System.EventArgs e)
        {
            _server.ShutdownAsync().Wait();
        }

        private void OnDesktopStartup(object? sender, ControlledApplicationLifetimeStartupEventArgs e)
        {
            _server.SetHostAndPort(CommunicationsConstants.MachineName, CommunicationsConstants.Port);
            _server.Start();
        }
    }
}
