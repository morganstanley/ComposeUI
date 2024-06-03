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

using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent;

namespace MorganStanley.ComposeUI.Shell.Fdc3.ResolverUi.Pages;

/// <summary>
/// Interaction logic for AdvancedResolverUiPage.xaml
/// </summary>
public partial class AdvancedResolverUiPage : Page
{
    public AdvancedResolverUiPage(IEnumerable<ResolverUiAppData> apps)
    {
        InitializeComponent();

        ResolverUiDataSource.ItemsSource = apps;
    }

    private void ResolverUiDataSource_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (ResolverUiDataSource.SelectedItem != null)
        {
            var window = Window.GetWindow(this);
            if (window is Fdc3ResolverUi resolverUiWindow)
            {
                resolverUiWindow.AppMetadata = ((ResolverUiAppData) ResolverUiDataSource.SelectedItem).AppMetadata;
                resolverUiWindow.Close();
            }
        }
    }
}
