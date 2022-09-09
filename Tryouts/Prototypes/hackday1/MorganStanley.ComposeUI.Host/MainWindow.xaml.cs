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

using MorganStanley.ComposeUI.Host.Modules;
using MorganStanley.ComposeUI.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace MorganStanley.ComposeUI.Host;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    List<IApplication> _apps = new List<IApplication>();
    CommunicationModule _communicationModule = new CommunicationModule();
    ModuleLoader _moduleLoader;

    public MainWindow()
    {
        InitializeComponent();
        _apps.Add(new DotNetCoreApplication("../../../../../Plugins/ApplicationPlugins/ComposeUI.Example.WPFDataGrid.TestApp/bin/Debug/ComposeUI.Example.WPFDataGrid.TestApp.dll"));
        _apps.Add(new ComposeHostedWebApplication("../../../../../Plugins/ApplicationPlugins/chart/src"));
        _apps.Add(new DotNetCoreApplication("../../../../../Plugins/ApplicationPlugins/ComposeUI.Example.DataService/bin/Debug/net6.0/ComposeUI.Example.DataService.dll"));
        _moduleLoader = new ModuleLoader(_apps, _communicationModule);
    }

    private void DisplayApps()
    {
        _apps[0].Render(null);
        _apps[1].Render(App2);
    }

    private async void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
    {
        // Ask apps if it's ok to close, can block indefinitely
        var closingRequests = new List<Task<bool>>();
        foreach (var app in _apps)
        {
            closingRequests.Add(app.ClosingRequested());
        }

        await Task.WhenAll(closingRequests);

        if (closingRequests.Any(x => !x.Result))
        {
            e.Cancel = true;
            return;
        }
        // Teardown apps, timeout after a while
        List<Task> teardownTasks = new List<Task>();

        foreach (var app in _apps)
        {
            teardownTasks.Add(app.Teardown());
        }

        await Task.WhenAny(Task.Delay(TimeSpan.FromSeconds(1)), Task.WhenAll(teardownTasks));

        // After all apps cleaned up, clean up the communication module
        await Task.WhenAny(Task.Delay(TimeSpan.FromSeconds(1)), _communicationModule.Teardown());

        _communicationModule.Dispose();
    }
        
    private async void Window_Loaded(object sender, RoutedEventArgs e)
    {
        await _moduleLoader.LoadModules();
        // Render apps
        await Dispatcher.InvokeAsync(DisplayApps);
    }
}
