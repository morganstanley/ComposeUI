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
using Shell.ImageSource;
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
        internal List<WebContent> WebContentList { get; set; } = new List<WebContent>();
        private ManifestModel _config;
        private ModuleModel[]? _modules;
        private ImageSourceProvider _iconProvider = new ImageSourceProvider(new EnvironmentImageSourcePolicy());

        public MainWindow()
        {
            InitializeComponent();

            _config = ManifestParser.OpenManifestFile("exampleManifest.json");
            _modules = _config.Modules;
            DataContext = _modules;
        }

        private void CreateViews(ModuleModel item)
        {
            var opt = new WebContentOptions()
            {
                Title = item.AppName,
                Uri = new Uri(item.Url),
                IconUri = string.IsNullOrEmpty(item.IconUrl) ? null : new Uri(item.IconUrl)
            };

            var webContent = new WebContent(opt, _iconProvider);            
            webContent.Owner = this;
            webContent.Closed += WebContent_Closed;

            WebContentList.Add(webContent);
            webContent.Show();
        }

        private void WebContent_Closed(object? sender, EventArgs e)
        {
            WebContentList.Remove((WebContent)sender);
        }

        private void ShowChild_Click(object sender, RoutedEventArgs e)
        {
            var context = ((Button)sender).DataContext;

            CreateViews((ModuleModel)context);
        }
    }
}
