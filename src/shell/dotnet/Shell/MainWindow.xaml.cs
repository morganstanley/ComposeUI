// /*
//  * Morgan Stanley makes this available to you under the Apache License,
//  * Version 2.0 (the "License"). You may obtain a copy of the License at
//  *
//  *      http://www.apache.org/licenses/LICENSE-2.0.
//  *
//  * See the NOTICE file distributed with this work for additional information
//  * regarding copyright ownership. Unless required by applicable law or agreed
//  * to in writing, software distributed under the License is distributed on an
//  * "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express
//  * or implied. See the License for the specific language governing permissions
//  * and limitations under the License.
//  */

using MorganStanley.ComposeUI.Shell.Manifest;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Ribbon;

namespace MorganStanley.ComposeUI.Shell;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : RibbonWindow
{
    internal List<WebWindow> WebWindows { get; set; } = new List<WebWindow>();
    private ManifestModel _config;
    private ModuleModel[]? _modules;

    public MainWindow()
    {
        InitializeComponent();

        _config = ManifestParser.OpenManifestFile("exampleManifest.json");
        _modules = _config.Modules;
        DataContext = _modules;
    }

    private void CreateWebWindow(ModuleModel item)
    {
        var options = new WebWindowOptions
        {
            Title = item.AppName,
            Url = item.Url,
            IconUrl = item.IconUrl
        };

        var webWindow = new WebWindow(options);
        webWindow.Owner = this;
        webWindow.Closed += WebWindowClosed;
        WebWindows.Add(webWindow);
        webWindow.Show();
    }

    private void WebWindowClosed(object? sender, EventArgs e)
    {
        WebWindows.Remove((WebWindow)sender!);
    }
    
    private void ShowChild_Click(object sender, RoutedEventArgs e)
    {
        var context = ((Button)sender).DataContext;
        
        CreateWebWindow((ModuleModel)context);
    }
}
