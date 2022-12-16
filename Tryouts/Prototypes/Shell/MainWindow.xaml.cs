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

        public MainWindow()
        {
            InitializeComponent();

            config = new ManifestParser().Manifest;
            modules = config.Modules;
            DataContext = modules;
        }

        private void CreateViews(object item) 
        {
            var webContent = new WebContent((item as string));

            webContent.Owner = this;

            webContent.Closed += WebContent_Closed;
            
            webContentList.Add(webContent);
            webContent.Show();
        }

        private void WebContent_Closed(object? sender, EventArgs e)
        {
            webContentList.Remove((WebContent)sender);
        }
        
        private void ShowChild_Click(object sender, RoutedEventArgs e)
        {
            this.CreateViews((sender as Button).Content);
        }
    }
}
