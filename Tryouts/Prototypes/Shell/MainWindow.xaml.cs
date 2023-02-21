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

using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using Manifest;

namespace Shell
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        internal List<WebWindow> webWindows { get; set; } = new List<WebWindow>();
        private ManifestModel config;
        private ModuleModel[]? modules;

        public MainWindow()
        {
            InitializeComponent();

            config = ManifestParser.OpenManifestFile("exampleManifest.json");
            modules = config.Modules;
            DataContext = modules;
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
            webWindows.Add(webWindow);
            webWindow.Show();
        }

        private void WebWindowClosed(object? sender, EventArgs e)
        {
            webWindows.Remove((WebWindow)sender!);
        }
        
        private void ShowChild_Click(object sender, RoutedEventArgs e)
        {
            var context = ((Button)sender).DataContext;
            
            CreateWebWindow((ModuleModel)context);
        }
    }
}
