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
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using Finos.Fdc3;
using MorganStanley.ComposeUI.Fdc3.DesktopAgent;

namespace MorganStanley.ComposeUI.Shell.Fdc3.ResolverUi.Pages;

/// <summary>
/// Interaction logic for SimpleResolverUiPage.xaml
/// </summary>
public partial class SimpleResolverUiPage : Page
{
    private readonly IEnumerable<ResolverUiAppData> _apps;

    public SimpleResolverUiPage(IEnumerable<ResolverUiAppData> apps)
    {
        InitializeComponent();

        _apps = apps;

        var collection = new ListCollectionView(apps.ToList());
        collection.GroupDescriptions.Add(new PropertyGroupDescription("AppId"));
        ResolverUiDataSource.ItemsSource = collection;
    }

    private void OpenApp_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as Button)?.CommandParameter is not string appId)
        {
            return;
        }

        var app = _apps.FirstOrDefault(x => x.AppId == appId && string.IsNullOrEmpty(x.AppMetadata.InstanceId));
        if (app == default)
        {
            return;
        }

        SetAppMetadataAndCloseWindow(app.AppMetadata);
    }

    private void ResolverUiDataSource_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (ResolverUiDataSource.SelectedItem != null)
        {
            SetAppMetadataAndCloseWindow(((ResolverUiAppData) ResolverUiDataSource.SelectedItem).AppMetadata);
        }
    }

    private void SetAppMetadataAndCloseWindow(IAppMetadata appMetadata)
    {
        var window = Window.GetWindow(this);
        if (window is Fdc3ResolverUi resolverUiWindow)
        {
            resolverUiWindow.AppMetadata = appMetadata;
            resolverUiWindow.Close();
        }
    }
}
