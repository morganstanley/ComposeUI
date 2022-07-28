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
using NP.Avalonia.Gidon;
using NP.NLogAdapter;

namespace MorganStanley.ComposeUI.Prototypes.NoPluginsPrototype
{
    public class App : Application
    {
        /// defined the Gidon plugin manager
        /// use the following paths (relative to the NoPluginsPrototype.exe executable)
        /// to dynamically load the plugins and services:
        /// "Plugins/Services" - to load the services (non-visual singletons)
        /// "Plugins/ViewModelPlugins" - to load view model plugins
        /// "Plugins/ViewPlugins" - to load view plugins
        public static PluginManager PluginManager { get; } =
            new PluginManager
            (
                "Plugins/Services",
                "Plugins/ViewModelPlugins",
                "Plugins/ViewPlugins");


        public App()
        {
            // inject a type from a statically loaded project NLogAdapter
            PluginManager.InjectType(typeof(NLogWrapper));

            // inject all dynamically loaded assemblies
            PluginManager.CompleteConfiguration();
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
            }

            base.OnFrameworkInitializationCompleted();
        }
    }
}
