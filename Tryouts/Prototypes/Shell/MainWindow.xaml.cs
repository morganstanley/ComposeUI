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

using Manifest;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Shell
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        internal List<WebContent> webContentList { get; set; } = new List<WebContent>();
        private ManifestModel config;
        private ModuleModel[]? modules;

        private string commandLineURL;
        private string[] commandLineArguments = (Application.Current as App).CommandLineArguments;

        public MainWindow()
        {
            InitializeComponent();

            this.Loaded += MainWindow_Loaded;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            if (this.commandLineArguments.Length != 0)
            {
                this.LazyLoading();
            }
            else
            {
                config = new ManifestParser().Manifest;
                modules = config.Modules;
                DataContext = modules;
            }
        }

        //potencial util stuff
        private void LazyLoading() {
            Dictionary<string, string> commands = new Dictionary<string, string>();

            this.commandLineArguments.ToList<string>().ForEach(item => {
                item = item.TrimStart('-');
                string[] command = item.Split("=");

                commands.Add(command[0], command[1]);
            });

            if (commands.ContainsKey("url"))
            {
                this.commandLineURL = commands["url"];
            }
            else 
            {
                this.commandLineURL = "about:blank";
            }

            var lazyWebContent = new WebContent(this.commandLineURL);
            if (commands.ContainsKey("width"))
            {
                lazyWebContent.Width = int.Parse(commands["width"]);
            }
            if (commands.ContainsKey("height"))
            {
                lazyWebContent.Height = int.Parse(commands["height"]);
            }
            lazyWebContent.Show();
        }

        //It needs to be called by the Main window (what ever way it was chosen) in case the case when the user wants child windows
        private void CreateViews(ModuleModel item) 
        {
            var webContent = new WebContent(item.Url);
            webContent.Title = item.AppName;

            webContent.Owner = this; //Main window

            webContent.Closed += WebContent_Closed;
            
            webContentList.Add(webContent);
            webContent.Show();
        }

        private void WebContent_Closed(object? sender, EventArgs e)
        {
            webContentList.Remove(sender as WebContent);
        }
        
        private void ShowChild_Click(object sender, RoutedEventArgs e)
        {
            var context = (sender as Button).DataContext;
            
            this.CreateViews((context as ModuleModel));
        }
    }
}
