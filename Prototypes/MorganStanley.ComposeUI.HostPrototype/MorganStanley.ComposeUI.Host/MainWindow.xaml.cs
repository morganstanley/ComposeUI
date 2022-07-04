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

using MorganStanley.ComposeUI.Host.Mock;
using MorganStanley.ComposeUI.Interfaces;
using MorganStanley.ComposeUI.Prototypes.WebAppHost;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using WpfDemoApp;

namespace MorganStanley.ComposeUI.Host;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private readonly ICommunicationModule _communicationModule;
    private readonly List<IApplication> _apps = new List<IApplication>();

    public MainWindow()
    {
        _apps.Add(new HelloWorldApp());
        _apps.Add(new WebApplication());
        _communicationModule = new CommunicationModuleMock();
        InitializeComponent();

    }

    private async Task InitializeApps()
    {
        var app1 = _apps[0].Initialize(_communicationModule.GetClient());
        var app2 = _apps[1].Initialize(_communicationModule.GetClient());
        await Task.WhenAll(app1, app2);
    }

    private void DisplayApps()
    {
        _apps[0].Render(App1);
        _apps[1].Render(App2);
    }

    private async void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
    {
        // Ask apps if it's ok to close, can block indefinitely
        var app1Task = _apps[0].ClosingRequested();
        var app2Task = _apps[1].ClosingRequested();

        await Task.WhenAll(app1Task, app2Task);

        if (!app1Task.Result || !app2Task.Result)
        {
            e.Cancel = true;
            return;
        }
        // Teardown apps, timeout after a while
        var app1Teardown = _apps[0].Teardown();
        var app2Teardown = _apps[1].Teardown();

        await Task.WhenAny(Task.Delay(TimeSpan.FromSeconds(1)), Task.WhenAll(app1Teardown, app2Teardown));

        // After all apps cleaned up, clean up the communication module
        await Task.WhenAny(Task.Delay(TimeSpan.FromSeconds(1)), _communicationModule.Teardown());
    }

    private async void Window_Initialized(object sender, EventArgs e)
    {
        // Initialize communication module
        await _communicationModule.Initialize(null);
        // Initialize apps
        await InitializeApps();
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        // Render apps (Consider switching to async and using the dispatcher? Seems a bit convoluted)
        DisplayApps();
    }
}
